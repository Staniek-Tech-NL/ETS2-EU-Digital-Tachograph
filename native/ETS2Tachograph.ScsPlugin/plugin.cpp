#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <cstdarg>
#include <cstdio>
#include <cstring>

#include "scssdk_telemetry.h"
#include "eurotrucks2/scssdk_eut2.h"
#include "eurotrucks2/scssdk_telemetry_eut2.h"

#include "telemetry_protocol.h"

namespace
{
    HANDLE mapping_handle = nullptr;
    ets2tachograph::telemetry_state* shared_state = nullptr;
    scs_log_t game_log = nullptr;

    std::uint8_t staged_running = 0;
    std::uint32_t staged_game_time = 0;
    float staged_speed = 0.0F;
    std::uint32_t staged_world_generation = 0;
    std::uint32_t staged_cargo_operation_generation = 0;
    bool staged_cargo_loaded = false;
    bool suppress_next_started_frame = false;

    void log_line(const scs_log_type_t type, const char* format, ...)
    {
        if (!game_log)
        {
            return;
        }

        char message[512]{};
        va_list arguments;
        va_start(arguments, format);
        vsnprintf_s(message, sizeof(message), _TRUNCATE, format, arguments);
        va_end(arguments);
        game_log(type, message);
    }

    void close_shared_memory()
    {
        if (shared_state)
        {
            UnmapViewOfFile(shared_state);
            shared_state = nullptr;
        }

        if (mapping_handle)
        {
            CloseHandle(mapping_handle);
            mapping_handle = nullptr;
        }
    }

    bool open_shared_memory()
    {
        mapping_handle = CreateFileMappingW(
            INVALID_HANDLE_VALUE,
            nullptr,
            PAGE_READWRITE,
            0,
            sizeof(ets2tachograph::telemetry_state),
            ets2tachograph::telemetry_mapping_name);

        if (!mapping_handle || GetLastError() == ERROR_ALREADY_EXISTS)
        {
            log_line(SCS_LOG_TYPE_error, "Unable to create telemetry shared memory (%lu).", GetLastError());
            close_shared_memory();
            return false;
        }

        shared_state = static_cast<ets2tachograph::telemetry_state*>(
            MapViewOfFile(mapping_handle, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(ets2tachograph::telemetry_state)));
        if (!shared_state)
        {
            log_line(SCS_LOG_TYPE_error, "Unable to map telemetry shared memory (%lu).", GetLastError());
            close_shared_memory();
            return false;
        }

        std::memset(shared_state, 0, sizeof(*shared_state));
        shared_state->magic = ets2tachograph::telemetry_magic;
        shared_state->version = ets2tachograph::telemetry_version;
        shared_state->size = sizeof(*shared_state);
        return true;
    }

    SCSAPI_VOID store_u32(
        const scs_string_t,
        const scs_u32_t,
        const scs_value_t* const value,
        const scs_context_t context)
    {
        auto* target = static_cast<std::uint32_t*>(context);
        *target = value && value->type == SCS_VALUE_TYPE_u32 ? value->value_u32.value : 0;
    }

    SCSAPI_VOID store_float(
        const scs_string_t,
        const scs_u32_t,
        const scs_value_t* const value,
        const scs_context_t context)
    {
        auto* target = static_cast<float*>(context);
        *target = value && value->type == SCS_VALUE_TYPE_float ? value->value_float.value : 0.0F;
    }

    SCSAPI_VOID telemetry_running(
        const scs_event_t event,
        const void* const,
        const scs_context_t)
    {
        if (event == SCS_TELEMETRY_EVENT_started)
        {
            staged_running = 1;
            // Job configuration/gameplay events produced by a loading screen
            // can arrive just after the first resumed frame. Do not publish
            // that frame before its cargo marker has had a chance to arrive.
            suppress_next_started_frame = true;
        }
        else
        {
            staged_running = 0;
        }
    }

    SCSAPI_VOID telemetry_frame_start(
        const scs_event_t,
        const void* const event_info,
        const scs_context_t)
    {
        const auto* frame = static_cast<const scs_telemetry_frame_start_t*>(event_info);
        if (frame && (frame->flags & SCS_TELEMETRY_FRAME_START_FLAG_timer_restart) != 0)
        {
            ++staged_world_generation;
        }
    }

    SCSAPI_VOID telemetry_configuration(
        const scs_event_t,
        const void* const event_info,
        const scs_context_t)
    {
        const auto* configuration = static_cast<const scs_telemetry_configuration_t*>(event_info);
        if (!configuration || std::strcmp(configuration->id, SCS_TELEMETRY_CONFIG_job) != 0)
        {
            return;
        }

        bool cargo_loaded_attribute_present = false;
        bool cargo_loaded = false;
        for (const scs_named_value_t* attribute = configuration->attributes;
             attribute && attribute->name;
             ++attribute)
        {
            if (std::strcmp(attribute->name, SCS_TELEMETRY_CONFIG_ATTRIBUTE_is_cargo_loaded) == 0 &&
                attribute->value.type == SCS_VALUE_TYPE_bool)
            {
                cargo_loaded_attribute_present = true;
                cargo_loaded = attribute->value.value_bool.value != 0;
                break;
            }
        }

        // An empty job configuration means that the previous job ended. The
        // next false -> true transition is the completion of cargo loading.
        if (!cargo_loaded_attribute_present)
        {
            staged_cargo_loaded = false;
            return;
        }

        if (cargo_loaded && !staged_cargo_loaded)
        {
            ++staged_cargo_operation_generation;
            log_line(
                SCS_LOG_TYPE_message,
                "Cargo loading marker advanced to %u.",
                staged_cargo_operation_generation);
        }
        staged_cargo_loaded = cargo_loaded;
    }

    SCSAPI_VOID telemetry_gameplay(
        const scs_event_t,
        const void* const event_info,
        const scs_context_t)
    {
        const auto* gameplay = static_cast<const scs_telemetry_gameplay_event_t*>(event_info);
        if (gameplay && std::strcmp(gameplay->id, SCS_TELEMETRY_GAMEPLAY_EVENT_job_delivered) == 0)
        {
            // job.delivered is emitted after the unloading screen. Its
            // generation marks that forward game-time jump as known work.
            ++staged_cargo_operation_generation;
            log_line(
                SCS_LOG_TYPE_message,
                "Cargo unloading marker advanced to %u.",
                staged_cargo_operation_generation);
        }
    }

    SCSAPI_VOID publish_frame(
        const scs_event_t,
        const void* const,
        const scs_context_t)
    {
        if (!shared_state)
        {
            return;
        }
        if (suppress_next_started_frame)
        {
            suppress_next_started_frame = false;
            return;
        }

        // Odd means writing, even means a stable snapshot (sequence lock).
        InterlockedIncrement(reinterpret_cast<volatile LONG*>(&shared_state->sequence));
        MemoryBarrier();
        shared_state->running = staged_running;
        shared_state->game_time_minutes = staged_game_time;
        shared_state->speed_meters_per_second = staged_speed;
        shared_state->world_generation = staged_world_generation;
        shared_state->cargo_operation_generation = staged_cargo_operation_generation;
        MemoryBarrier();
        InterlockedIncrement(reinterpret_cast<volatile LONG*>(&shared_state->sequence));
    }
}

SCSAPI_RESULT scs_telemetry_init(
    const scs_u32_t version,
    const scs_telemetry_init_params_t* const params)
{
    if (version != SCS_TELEMETRY_VERSION_1_01)
    {
        return SCS_RESULT_unsupported;
    }

    const auto* version_params = static_cast<const scs_telemetry_init_params_v101_t*>(params);
    game_log = version_params->common.log;

    if (std::strcmp(version_params->common.game_id, SCS_GAME_ID_EUT2) != 0)
    {
        log_line(SCS_LOG_TYPE_error, "This plugin supports Euro Truck Simulator 2 only.");
        game_log = nullptr;
        return SCS_RESULT_unsupported;
    }

    if (!open_shared_memory())
    {
        game_log = nullptr;
        return SCS_RESULT_generic_error;
    }

    const bool events_registered =
        version_params->register_for_event(SCS_TELEMETRY_EVENT_started, telemetry_running, nullptr) == SCS_RESULT_ok &&
        version_params->register_for_event(SCS_TELEMETRY_EVENT_paused, telemetry_running, nullptr) == SCS_RESULT_ok &&
        version_params->register_for_event(SCS_TELEMETRY_EVENT_frame_start, telemetry_frame_start, nullptr) == SCS_RESULT_ok &&
        version_params->register_for_event(SCS_TELEMETRY_EVENT_frame_end, publish_frame, nullptr) == SCS_RESULT_ok &&
        version_params->register_for_event(SCS_TELEMETRY_EVENT_configuration, telemetry_configuration, nullptr) == SCS_RESULT_ok &&
        version_params->register_for_event(SCS_TELEMETRY_EVENT_gameplay, telemetry_gameplay, nullptr) == SCS_RESULT_ok;

    const bool channels_registered =
        version_params->register_for_channel(
            SCS_TELEMETRY_CHANNEL_game_time,
            SCS_U32_NIL,
            SCS_VALUE_TYPE_u32,
            SCS_TELEMETRY_CHANNEL_FLAG_no_value,
            store_u32,
            &staged_game_time) == SCS_RESULT_ok &&
        version_params->register_for_channel(
            SCS_TELEMETRY_TRUCK_CHANNEL_speed,
            SCS_U32_NIL,
            SCS_VALUE_TYPE_float,
            SCS_TELEMETRY_CHANNEL_FLAG_no_value,
            store_float,
            &staged_speed) == SCS_RESULT_ok;

    if (!events_registered || !channels_registered)
    {
        log_line(SCS_LOG_TYPE_error, "Unable to register required telemetry callbacks.");
        close_shared_memory();
        game_log = nullptr;
        return SCS_RESULT_generic_error;
    }

    log_line(SCS_LOG_TYPE_message, "ETS2 Tachograph telemetry plugin initialized (protocol v3).");
    return SCS_RESULT_ok;
}

SCSAPI_VOID scs_telemetry_shutdown()
{
    close_shared_memory();
    staged_running = 0;
    staged_game_time = 0;
    staged_speed = 0.0F;
    staged_world_generation = 0;
    staged_cargo_operation_generation = 0;
    staged_cargo_loaded = false;
    suppress_next_started_frame = false;
    game_log = nullptr;
}

BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID)
{
    return TRUE;
}
