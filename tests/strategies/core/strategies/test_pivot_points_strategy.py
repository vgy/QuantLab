import pytest
import pandas as pd

from strategies.core.strategies.pivotpoints_strategy import PivotPointsStrategy

@pytest.fixture
def sample_df():
    return pd.DataFrame({
        "Symbol": ["AAPL"] * 5,
        "Close": [100, 101, 102, 103, 104],
    })

@pytest.fixture
def mock_config(mocker, tmp_path):
    mock_dir = tmp_path / "data"
    (mock_dir / "1d").mkdir(parents=True)

    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.load_config",
        return_value={"data": {"directory": str(mock_dir)}}
    )
    return mock_dir

def test_pivot_points_FileMissing_ReturnsNone(mocker, mock_config):
    # Arrange
    symbol = "AAPL"
    # file not created → tested default case

    # Act
    result = PivotPointsStrategy._pivot_points(symbol)

    # Assert
    assert result is None

def test_pivot_points_FileExists_ComputesCorrectValues(mocker, mock_config, tmp_path):
    # Arrange
    symbol = "AAPL"
    directory = mock_config
    folder_path = directory / "1d"
    file_path = folder_path / f"1d-{symbol}.csv"

    df = pd.DataFrame({
        "High": [10, 20],
        "Low": [5, 12],
        "Close": [7, 15]
    })
    df.to_csv(file_path, index=False)

    # Act
    pivots = PivotPointsStrategy._pivot_points(symbol)

    # Assert (manual calculation for prev_day = row index -2: High=10, Low=5, Close=7)
    pp = (10 + 5 + 7) / 3
    assert pivots["PP"] == pp
    assert pivots["R1"] == 2 * pp - 5
    assert pivots["S1"] == 2 * pp - 10
    assert pivots["R2"] == pp + (10 - 5)
    assert pivots["S2"] == pp - (10 - 5)
    assert pivots["R3"] == 10 + 2 * (pp - 5)
    assert pivots["S3"] == 5 - 2 * (10 - pp)

def test_pivot_points_MissingColumns_RaisesKeyError(mock_config, tmp_path):
    # Arrange
    symbol = "AAPL"
    directory = mock_config
    folder_path = directory / "1d"
    file_path = folder_path / f"1d-{symbol}.csv"

    df = pd.DataFrame({
        "High": [10, 20],
        "Low": [5, 12]
    })  # Missing Close
    df.to_csv(file_path, index=False)

    # Act / Assert
    with pytest.raises(KeyError):
        PivotPointsStrategy._pivot_points(symbol)

def test_is_last_close_near_pivotpoints_NoPivots_ReturnsFalse(mocker, sample_df):
    # Arrange
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value=None
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(sample_df)

    # Assert
    assert result is False

def test_is_last_close_near_pivotpoints_CloseNearPP_ReturnsTrue(mocker, sample_df):
    # Arrange
    mock_pivots = {"PP": 104.5}
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value=mock_pivots
    )
    # latest close = 104 → diff = 0.5 → pct ~0.0048 < tolerance

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(
        sample_df,
        tolerance=0.01
    )

    # Assert
    assert result is True

def test_is_last_close_near_pivotpoints_CloseNotNearAny_ReturnsFalse(mocker, sample_df):
    # Arrange
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": 200, "R1": 300}
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(sample_df)

    # Assert
    assert result is False

def test_is_last_close_near_pivotpoints_OnlySpecificLevelsChecked_ReturnsTrue(mocker, sample_df):
    # Arrange
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": 104.5, "S1": 50}
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(
        sample_df,
        levels=["PP"]
    )

    # Assert
    assert result is True

def test_is_last_close_near_pivotpoints_InvalidLevel_IgnoredAndReturnsFalse(mocker, sample_df):
    # Arrange
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": 200}
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(
        sample_df,
        levels=["R1"]  # not present
    )

    # Assert
    assert result is False

@pytest.mark.parametrize(
    "pivot, close, tolerance, expected",
    [
        (100, 100, 0.01, True),    # exact match
        (101, 100, 0.02, True),    # within 1%
        (110, 100, 0.05, False),   # too far
        (98, 100, 0.015, False),    # slightly below
    ]
)
def test_is_last_close_near_pivotpoints_Parametrized(
    mocker, pivot, close, tolerance, expected
):
    # Arrange
    df = pd.DataFrame({"Symbol": ["AAPL"], "Close": [close]})

    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": pivot}
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(
        df,
        tolerance=tolerance
    )

    # Assert
    assert result == expected

def test_is_last_close_near_pivotpoints_MultipleLevelsOnlyOneMatches_ReturnsTrue(
    mocker, sample_df
):
    # Arrange
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": 104.5, "S1": 50, "R1": 500}
    )

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(sample_df)

    # Assert
    assert result is True

def test_is_last_close_near_pivotpoints_ToleranceBoundary_ReturnsTrue(mocker, sample_df):
    # Arrange
    last_close = 104
    pivot = 104 * 1.009
    mocker.patch(
        "strategies.core.strategies.pivotpoints_strategy.PivotPointsStrategy._pivot_points",
        return_value={"PP": pivot}
    )

    df = sample_df.copy()
    df.loc[len(df)-1, "Close"] = last_close

    # Act
    result = PivotPointsStrategy.is_last_close_near_pivotpoints(df, tolerance=0.01)

    # Assert
    assert result is True
