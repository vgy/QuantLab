from typing import Dict, List, Tuple
from types import MappingProxyType

# -------------------------------
# Candlestick Patterns Dictionary
# -------------------------------
_candlestick_patterns: Dict[str, Dict[str, List[Tuple[str, int]]]] = {
    "bullish": {
        "reversal": [
            ("cdl3whitesoldiers", 1),
            ("cdlmorningstar", 2),
            ("cdlmorningdojistar", 3),
            ("cdlpiercing", 4),
            ("cdlengulfing", 5),
            ("cdlhammer", 6),
            ("cdlinvertedhammer", 7),
            ("cdlladderbottom", 8),
            ("cdlunique3river", 9),
            ("cdlhomingpigeon", 10),
            ("cdlmatchinglow", 11),
            ("cdl3inside", 12),
            ("cdl3outside", 13)
        ],
        "continuation": [
            ("cdlmathold", 1),
            ("cdlrisefall3methods", 2),
            ("cdltasukigap", 3),
            ("cdlseparatinglines", 4)
        ],
    },
    "bearish": {
        "reversal": [
            ("cdl3blackcrows", 1),
            ("cdleveningstar", 2),
            ("cdleveningdojistar", 3),
            ("cdlengulfing", 4),
            ("cdldarkcloudcover", 5),
            ("cdlshootingstar", 6),
            ("cdlhangingman", 7),
            ("cdlidentical3crows", 8),
            ("cdl2crows", 9),
            ("cdlharami", 10),
            ("cdlharamicross", 11),
            ("cdl3inside", 12),
            ("cdl3outside", 13)
        ],
        "continuation": [
            ("cdlrisefall3methods", 1),
            ("cdlupsidegap2crows", 2),
            ("cdlgapsidesidewhite", 3),
            ("cdlthrusting", 4),
            ("cdlonneck", 5),
            ("cdlinneck", 6)
        ]
    },
    "neutral": {
        "doji": [
            ("cdldoji", 1),
            ("cdllongleggeddoji", 2),
            ("cdldragonflydoji", 3),
            ("cdlgravestonedoji", 4),
            ("cdlrickshawman", 5)
        ],
        "spinning / range": [
            ("cdlspinningtop", 1),
            ("cdlshortline", 2),
            ("cdllongline", 3),
            ("cdlhighwave", 4)
        ],
        "marubozu": [
            ("cdlmarubozu", 1)
        ],
        "hikkake": [
            ("cdlhikkake", 1),
            ("cdlhikkakemod", 2)
        ],
        "side-by-side / gap methods": [
            ("cdlgapsidesidewhite", 1),
            ("cdlxsidegap3methods", 2)
        ],
        "breakaway / counterattack / stalled": [
            ("cdlbreakaway", 1),
            ("cdlcounterattack", 2),
            ("cdlstalledpattern", 3),
            ("cdltakuri", 4)
        ],
        "tri-star & special": [
            ("cdltristar", 1),
            ("cdlconcealbabyswall", 2)
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

_bullish_reversal_patterns = [
    name
    for (name, _) in _candlestick_patterns["bullish"]["reversal"]
]

_bullish_continuation_patterns = [
    name
    for (name, _) in _candlestick_patterns["bullish"]["continuation"]
]

_bearish_reversal_patterns = [
    name
    for (name, _) in _candlestick_patterns["bearish"]["reversal"]
]

_bearish_continuation_patterns = [
    name
    for (name, _) in _candlestick_patterns["bearish"]["continuation"]
]

_neutral_doji_patterns = [
    name
    for (name, _) in _candlestick_patterns["neutral"]["doji"]
]

_bullish_patterns = [
    name
    for subgroup in _candlestick_patterns["bullish"].values()
    for (name, _) in subgroup
]

_bearish_patterns = [
    name
    for subgroup in _candlestick_patterns["bearish"].values()
    for (name, _) in subgroup
]

_neutral_patterns = [
    name
    for subgroup in _candlestick_patterns["neutral"].values()
    for (name, _) in subgroup
]

_all_patterns = list({name for group in _candlestick_patterns.values()
                      for subgroup in group.values()
                      for (name, _) in subgroup})

patterns_map = {
    "bullish": {
        "reversal": _bullish_reversal_patterns,
        "continuation": _bullish_continuation_patterns,
        "all": _bullish_patterns,
    },
    "bearish": {
        "reversal": _bearish_reversal_patterns,
        "continuation": _bearish_continuation_patterns,
        "all": _bearish_patterns,
    },
    "neutral": {
        "doji": _neutral_doji_patterns,
        "all": _neutral_patterns,
    }
}


# Make them immutable (strongly recommended)
candlestick_patterns = MappingProxyType(_candlestick_patterns)
reverse_lookup = MappingProxyType(_reverse_lookup)

def get_patterns(group: str, subgroup: str, pattern: str) -> List[str]:
    if group == "all":
        return _all_patterns
    elif subgroup == "all":
        return patterns_map[group]["all"]
    elif pattern == "all":
        return patterns_map[group][subgroup]
    else:
        return [pattern] if pattern in reverse_lookup else []
