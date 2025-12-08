import pytest
import numpy as np
import pandas as pd
from strategies.core.strategies.bollingerbands_strategy import BollingerBandsStrategy


def make_df(close_values):
    return pd.DataFrame({"Close": close_values})

def test_is_near_lower_bb_DataTooShort_ReturnsFalse():
    # Arrange
    df = make_df([100])  # period = 12, duration = 12 → too short

    # Act
    result = BollingerBandsStrategy.is_near_lower_bb(df)

    # Assert
    assert result is False


@pytest.mark.parametrize(
    "close, lower, expected",
    [
        # (close near lower BB → within tolerance → True)
        ([100, 100, 100], [99, 99.5, 99.8], True),

        # not near (too far) → False
        ([100, 100, 100], [90, 90, 90], False),

        # exactly at tolerance → True
        ([100], [99], True),

        # slightly outside tolerance → False
        ([100], [98], False),
    ]
)
def test_is_near_lower_bb_ClosenessCheck_WorksAsExpected(mocker, close, lower, expected):
    # Arrange
    df = make_df(close)

    # Mock BBANDS: only lower band matters for this test
    upper = np.array(close) + 10
    middle = np.array(close)
    lower = np.array(lower)

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(upper, middle, lower)
    )

    # Act
    result = BollingerBandsStrategy.is_near_lower_bb(df, period=1, duration=len(close), tolerance=0.01)

    # Assert
    assert result == expected


def test_is_near_lower_bb_DurationGreaterThanDF_ClampsToLength(mocker):
    # Arrange
    close = [100, 101, 103]
    df = make_df(close)

    upper = np.array(close) + 10
    middle = np.array(close)
    lower = np.array([99, 99.5, 99.8])

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(upper, middle, lower)
    )

    # Act
    result = BollingerBandsStrategy.is_near_lower_bb(df, period=1, duration=3, tolerance=0.01)

    # Assert
    assert result  # lower band is close


def test_is_near_lower_bb_MultipleBars_AnyBarTriggersTrue(mocker):
    # Arrange
    close = [100, 102, 105, 110]
    lower = [99, 100, 104.5, 109.9]  # last value within tolerance

    df = make_df(close)
    upper = np.array(close) + 10
    middle = np.array(close)

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(upper, middle, np.array(lower))
    )

    # Act
    result = BollingerBandsStrategy.is_near_lower_bb(df, period=1, duration=4, tolerance=0.02)

    # Assert
    assert result
    

def test_is_near_upper_bb_DataTooShort_ReturnsFalse():
    # Arrange
    df = make_df([100])

    # Act
    result = BollingerBandsStrategy.is_near_upper_bb(df)

    # Assert
    assert result is False


@pytest.mark.parametrize(
    "close, upper, expected",
    [
        # close near upper BB → True
        ([100, 101, 102], [100.5, 101.2, 102.9], True),

        # far from upper → False
        ([100, 101, 102], [120, 121, 122], False),

        # exactly at tolerance → True
        ([100], [101], True),

        # slightly outside tolerance → False
        ([100], [102], False),
    ]
)
def test_is_near_upper_bb_ClosenessCheck_WorksAsExpected(mocker, close, upper, expected):
    # Arrange
    df = make_df(close)

    upper = np.array(upper)
    middle = np.array(close)
    lower = np.array(close) - 10

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(upper, middle, lower)
    )

    # Act
    result = BollingerBandsStrategy.is_near_upper_bb(df, period=1, duration=len(close), tolerance=0.01)

    # Assert
    assert result == expected


def test_is_near_upper_bb_DurationGreaterThanDF_ClampsToLength(mocker):
    # Arrange
    close = [100, 101, 102]
    upper = [100.5, 101.3, 103]  # last bar triggers True

    df = make_df(close)
    middle = np.array(close)
    lower = np.array(close) - 10

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(np.array(upper), middle, lower)
    )

    # Act
    result = BollingerBandsStrategy.is_near_upper_bb(df, period=1, duration=3, tolerance=0.02)

    # Assert
    assert result


def test_is_near_upper_bb_MultipleBars_AnyBarTriggersTrue(mocker):
    # Arrange
    close = [100, 102, 105, 110]
    upper = [101, 104, 107, 110.5]  # last one within tolerance

    df = make_df(close)
    middle = np.array(close)
    lower = np.array(close) - 10

    mocker.patch(
        "strategies.core.strategies.bollingerbands_strategy.ta.BBANDS",
        return_value=(np.array(upper), middle, lower)
    )

    # Act
    result = BollingerBandsStrategy.is_near_upper_bb(df, period=1, duration=4, tolerance=0.02)

    # Assert
    assert result
