export const pipelineTemplates = {
  bullish: `{
  "strategies": [
    {"strategy":"contains_candlestick_pattern","interval":"1h","params":{"group":"bullish","subgroup":"all","pattern":"all","duration":1}},
    {"strategy":"is_last_close_near_pivotpoints","interval":"1h"},
    {"strategy":"is_rsi_oversold","interval":"5min","params":{"duration":12}},
    {"strategy":"is_near_lower_bb","interval":"5min","params":{"duration":12}},
    {"strategy":"is_bullish_macd_crossover","interval":"5min","params":{"duration":12}},
    {"strategy":"is_rsi_bullish_divergence","interval":"5min","params":{"duration":12}},
    {"strategy":"is_failed_brbo","interval":"5min"}
  ]
}`,
  bearish: `{
  "strategies": [
    {"strategy":"contains_candlestick_pattern","interval":"1h","params":{"group":"bearish","subgroup":"all","pattern":"all","duration":1}},
    {"strategy":"is_last_close_near_pivotpoints","interval":"1h"},
    {"strategy":"is_rsi_overbought","interval":"5min","params":{"duration":12}},
    {"strategy":"is_near_upper_bb","interval":"5min","params":{"duration":12}},
    {"strategy":"is_bearish_macd_crossover","interval":"5min","params":{"duration":12}},
    {"strategy":"is_rsi_bearish_divergence","interval":"5min","params":{"duration":12}},
    {"strategy":"is_failed_blbo","interval":"5min"}
  ]
}`,
};
