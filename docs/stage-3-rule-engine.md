# Stage 3 - RuleEngine scope

The RuleEngine is a pure projection over immutable activity history. It implements the
freight-transport baseline of Regulation (EC) 561/2006:

- Article 6: 9-hour daily driving, two 10-hour extensions per fixed week, 56 hours per
  week and 90 hours across two consecutive weeks;
- Article 7: 4 hours 30 minutes continuous driving, a 45-minute break or the ordered
  15+30-minute split;
- Article 8: daily rest within 24 hours (30 hours for multi-manning), no more than three
  reduced daily rests between weekly rests, weekly rest by the end of six 24-hour
  periods, the two-week weekly-rest pattern and reduced-weekly-rest compensation;
- Article 9 ferry handling remains in Core and feeds valid rest segments to the engine.

Not included in v1: occasional passenger-service derogations, Article 12 emergency
departures, and the international-goods derogation allowing two consecutive reduced
weekly rests. These require explicit trip declarations and must not be inferred from ETS2
telemetry.

Compensation projection is conservative. A reduced weekly rest creates an obligation.
Only time above 45 hours in a later regular weekly rest, or above 11 hours in another
qualifying rest, is automatically allocated to the oldest obligation.

The diagnostic monitor evaluates only its current in-memory session. Cross-session
history and durable counters will be supplied by Stage 4 persistence.
