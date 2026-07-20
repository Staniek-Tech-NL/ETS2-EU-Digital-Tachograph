#pragma once

#include <cstdint>

namespace ets2tachograph
{
    constexpr std::uint32_t telemetry_magic = 0x32535445; // "ETS2" in little endian.
    constexpr std::uint16_t telemetry_version = 3;
    constexpr wchar_t telemetry_mapping_name[] = L"Local\\ETS2Tachograph.Telemetry.v3";

#pragma pack(push, 1)
    struct telemetry_state
    {
        std::uint32_t magic;
        std::uint16_t version;
        std::uint16_t size;
        volatile std::uint32_t sequence;
        std::uint8_t running;
        std::uint8_t reserved[3];
        std::uint32_t game_time_minutes;
        float speed_meters_per_second;
        std::uint32_t world_generation;
        std::uint32_t cargo_operation_generation;
    };
#pragma pack(pop)

    static_assert(sizeof(telemetry_state) == 32, "The .NET reader requires a 32-byte protocol.");
}
