import pandas as pd
import pytest
from strategies.core.downsampling_service import DownsamplingService


# ------------------------------------------------------------------------------
# Fixtures
# ------------------------------------------------------------------------------

@pytest.fixture
def mock_load_config(mocker, tmp_path):
    """Ensures DownsamplingService loads a predictable directory."""
    return mocker.patch(
        "strategies.core.downsampling_service.load_config",
        return_value={"data": {"directory": str(tmp_path)}}
    )


@pytest.fixture
def mock_logger(mocker):
    return mocker.patch("strategies.core.downsampling_service.logger")


@pytest.fixture
def sample_df():
    data = {
        "Symbol": ["AAPL"] * 4,
        "Interval": ["1m"] * 4,
        "Timestamp": [
            "2024-01-02 09:00:00",
            "2024-01-02 09:01:00",
            "2024-01-02 09:02:00",
            "2024-01-02 09:03:00",
        ],
        "Open": [100, 101, 102, 103],
        "High": [101, 102, 103, 104],
        "Low": [99, 100, 101, 102],
        "Close": [101, 102, 103, 104],
        "Volume": [10, 20, 30, 40],
    }
    return pd.DataFrame(data)


# ------------------------------------------------------------------------------
# Constructor Tests
# ------------------------------------------------------------------------------

def test_constructor_ConfigLoads_InitializesDirectory(mock_load_config, mock_logger):
    # Arrange / Act
    service = DownsamplingService()

    # Assert
    assert service.directory is not None
    mock_load_config.assert_called_once()
    mock_logger.info.assert_called_once()


# ------------------------------------------------------------------------------
# downsample()
# ------------------------------------------------------------------------------

@pytest.mark.parametrize(
    "interval,offset",
    [
        ("5min", "0min"),
        ("1h", "1min"),
        ("30min", "2min")
    ]
)
def test_downsample_ValidInput_ProducesCorrectResampling(mocker, mock_logger, sample_df, interval, offset):
    # Arrange
    service = DownsamplingService()
    mocker.patch("strategies.core.downsampling_service.logger")  # silence logs inside downsample

    # Act
    result = service.downsample(sample_df.copy(), interval=interval, offset=offset)

    # Assert
    assert not result.empty
    assert "Symbol" in result.columns
    assert "Timestamp" in result.columns
    assert result.iloc[0]["Interval"] == interval


def test_downsample_RemovesOutsideTradingHours(mocker, mock_logger):
    # Arrange
    service = DownsamplingService()
    df = pd.DataFrame({
        "Symbol": ["AAPL", "AAPL", "AAPL"],
        "Interval": ["1m", "1m", "1m"],
        "Timestamp": [
            "2024-01-06 19:00:00",
            "2024-01-07 18:00:00",
            "2024-01-02 08:30:00" 
        ],
        "Open": [1, 2, 3],
        "High": [1, 2, 3],
        "Low": [1, 2, 3],
        "Close": [1, 2, 3],
        "Volume": [10, 20, 30]
    })

    # Act
    result = service.downsample(df, interval="1h")

    # Assert
    assert result.empty  # all excluded by filters


# ------------------------------------------------------------------------------
# write_downsampling()
# ------------------------------------------------------------------------------

def test_write_downsampling_ValidInput_WritesFilesSuccessfully(
    mocker, mock_load_config, mock_logger, sample_df
):
    # Arrange
    service = DownsamplingService()

    mock_read_all = mocker.patch(
        "strategies.core.downsampling_service.FileService.read_all_csv",
        return_value=[sample_df]
    )

    mock_write = mocker.patch(
        "strategies.core.downsampling_service.FileService.write"
    )

    mock_downsample = mocker.patch.object(
        DownsamplingService,
        "downsample",
        return_value=sample_df
    )

    # Act
    output = service.write_downsampling("1m", "1h")

    # Assert
    assert "Downsampled from 1m to 1h" in output
    mock_read_all.assert_called_once_with("1m")
    mock_downsample.assert_called_once()
    mock_write.assert_called_once()  # writes 1 CSV file
    mock_logger.info.assert_called()


def test_write_downsampling_MultipleFiles_WritesEach(mocker, mock_load_config, mock_logger, sample_df):
    # Arrange
    service = DownsamplingService()

    mock_read_all = mocker.patch(
        "strategies.core.downsampling_service.FileService.read_all_csv",
        return_value=[sample_df, sample_df, sample_df]  # 3 dfs
    )

    mock_downsample = mocker.patch.object(
        DownsamplingService,
        "downsample",
        return_value=sample_df
    )

    mock_write = mocker.patch("strategies.core.downsampling_service.FileService.write")

    # Act
    service.write_downsampling("1m", "1h")

    # Assert
    assert mock_write.call_count == 3
    assert mock_downsample.call_count == 3


def test_write_downsampling_ReadFails_RaisesAndLogs(mocker, mock_load_config, mock_logger):
    # Arrange
    service = DownsamplingService()

    mocker.patch(
        "strategies.core.downsampling_service.FileService.read_all_csv",
        side_effect=RuntimeError("read fail")
    )

    # Act / Assert
    with pytest.raises(RuntimeError):
        service.write_downsampling("1m", "1h")

    mock_logger.exception.assert_called_once()


def test_write_downsampling_DownsampleFails_RaisesAndLogs(mocker, mock_load_config, mock_logger, sample_df):
    # Arrange
    service = DownsamplingService()

    mocker.patch(
        "strategies.core.downsampling_service.FileService.read_all_csv",
        return_value=[sample_df]
    )

    mocker.patch.object(
        DownsamplingService,
        "downsample",
        side_effect=ValueError("bad data")
    )

    # Act / Assert
    with pytest.raises(ValueError):
        service.write_downsampling("1m", "1h")

    mock_logger.exception.assert_called_once()


def test_write_downsampling_WriteFails_RaisesAndLogs(mocker, mock_load_config, mock_logger, sample_df):
    # Arrange
    service = DownsamplingService()

    mocker.patch(
        "strategies.core.downsampling_service.FileService.read_all_csv",
        return_value=[sample_df]
    )

    mocker.patch.object(
        DownsamplingService,
        "downsample",
        return_value=sample_df
    )

    mocker.patch(
        "strategies.core.downsampling_service.FileService.write",
        side_effect=IOError("write fail")
    )

    # Act / Assert
    with pytest.raises(IOError):
        service.write_downsampling("1m", "1h")

    mock_logger.exception.assert_called_once()
