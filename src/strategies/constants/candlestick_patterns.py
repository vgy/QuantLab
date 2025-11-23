from typing import Dict, List, Tuple
from types import MappingProxyType

# -------------------------------
# Candlestick Patterns Dictionary
# -------------------------------
_candlestick_patterns: Dict[str, Dict[str, List[Tuple[str, int]]]] = {
    "Bullish": {
        "Reversal": [
            ("CDL3WHITESOLDIERS", 1),
            ("CDLMORNINGSTAR", 2),
            ("CDLMORNINGDOJISTAR", 3),
            ("CDLPIERCING", 4),
            ("CDLENGULFING", 5),
            ("CDLHAMMER", 6),
            ("CDLINVERTEDHAMMER", 7),
            ("CDLLADDERBOTTOM", 8),
            ("CDLUNIQUE3RIVER", 9),
            ("CDLHOMINGPIGEON", 10),
            ("CDLMATCHINGLOW", 11),
            ("CDL3INSIDE", 12),
            ("CDL3OUTSIDE", 13)
        ],
        "Continuation": [
            ("CDLMATHOLD", 1),
            ("CDLRISEFALL3METHODS", 2),
            ("CDLTASUKIGAP", 3),
            ("CDLSEPARATINGLINES", 4)
        ],
    },
    "Bearish": {
        "Reversal": [
            ("CDL3BLACKCROWS", 1),
            ("CDLEVENINGSTAR", 2),
            ("CDLEVENINGDOJISTAR", 3),
            ("CDLENGULFING", 4),
            ("CDLDARKCLOUDCOVER", 5),
            ("CDLSHOOTINGSTAR", 6),
            ("CDLHANGINGMAN", 7),
            ("CDLIDENTICAL3CROWS", 8),
            ("CDL2CROWS", 9),
            ("CDLHARAMI", 10),
            ("CDLHARAMICROSS", 11),
            ("CDL3INSIDE", 12),
            ("CDL3OUTSIDE", 13)
        ],
        "Continuation": [
            ("CDLRISEFALL3METHODS", 1),
            ("CDLUPSIDEGAP2CROWS", 2),
            ("CDLGAPSIDESIDEWHITE", 3),
            ("CDLTHRUSTING", 4),
            ("CDLONNECK", 5),
            ("CDLINNECK", 6)
        ]
    },
    "Neutral": {
        "Doji": [
            ("CDLDOJI", 1),
            ("CDLLONGLEGGEDDOJI", 2),
            ("CDLDRAGONFLYDOJI", 3),
            ("CDLGRAVESTONEDOJI", 4),
            ("CDLRICKSHAWMAN", 5)
        ],
        "Spinning / Range": [
            ("CDLSPINNINGTOP", 1),
            ("CDLSHORTLINE", 2),
            ("CDLLONGLINE", 3),
            ("CDLHIGHWAVE", 4)
        ],
        "Marubozu": [
            ("CDLMARUBOZU", 1)
        ],
        "Hikkake": [
            ("CDLHIKKAKE", 1),
            ("CDLHIKKAKEMOD", 2)
        ],
        "Side-by-Side / Gap Methods": [
            ("CDLGAPSIDESIDEWHITE", 1),
            ("CDLXSIDEGAP3METHODS", 2)
        ],
        "Breakaway / Counterattack / Stalled": [
            ("CDLBREAKAWAY", 1),
            ("CDLCOUNTERATTACK", 2),
            ("CDLSTALLEDPATTERN", 3),
            ("CDLTAKURI", 4)
        ],
        "Tri-Star & Special": [
            ("CDLTRISTAR", 1),
            ("CDLCONCEALBABYSWALL", 2)
        ]
    }
}

# -------------------------------
# Reverse Lookup Dictionary
# -------------------------------
_reverse_lookup: Dict[str, Dict[str, str | int]] = {}

for group, subgroups in _candlestick_patterns.items():
    for subgroup, patterns in subgroups.items():
        for pattern, rank in patterns:
            _reverse_lookup[pattern] = {
                "group": group,
                "subgroup": subgroup,
                "rank": rank
            }

# Make them immutable (strongly recommended)
candlestick_patterns = MappingProxyType(_candlestick_patterns)
reverse_lookup = MappingProxyType(_reverse_lookup)
