import pandas as pd
import pytest
from strategies.core.file_service import FileService


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

@pytest.fixture
def sample_df():
    return pd.DataFrame({"a": [1, 2], "b": [3, 4]})


@pytest.fixture
def mock_load_config(mocker, tmp_path):
    """
    Ensures FileService.__get_directory() always returns tmp_path.
    """
    return mocker.patch(
        "strategies.core.file_service.load_config",
        return_value={"data": {"directory": str(tmp_path)}}
    )


# ---------------------------------------------------------------------------
# write()
# ---------------------------------------------------------------------------
@pytest.mark.parametrize(
    "interval,filename",
    [
        ("5min", "5min-ABC.csv"),
        ("15min", "15min-ABC.csv"),
        ("1h", "1h-ABC.csv"),
        ("1d", "1d-ABC.csv"),
    ],
)
def test_write_ValidInput_WritesFileSuccessfully(mocker, sample_df, mock_load_config, tmp_path, interval,filename):
    # Arrange
    mock_makedirs = mocker.patch("os.makedirs")
    mock_to_csv = mocker.patch.object(pd.DataFrame, "to_csv")
    mock_logger = mocker.patch("strategies.core.file_service.logger")

    # Act
    FileService.write(interval, sample_df, filename)

    # Assert
    expected_folder = tmp_path / interval
    mock_makedirs.assert_called_once_with(str(expected_folder), exist_ok=True)
    mock_to_csv.assert_called_once_with(str(expected_folder / filename), index=False)
    mock_logger.info.assert_called_once()


def test_write_MakedirsFails_ExceptionLoggedAndReraised(mocker, sample_df, mock_load_config):
    # Arrange
    mocker.patch("os.makedirs", side_effect=OSError("mkdir fail"))
    mock_logger = mocker.patch("strategies.core.file_service.logger")

    # Act / Assert
    with pytest.raises(Exception):
        FileService.write("1m", sample_df, "f.csv")

    mock_logger.exception.assert_called_once()


def test_write_ToCsvFails_ExceptionLoggedAndReraised(mocker, sample_df, mock_load_config, tmp_path):
    # Arrange
    mocker.patch("os.makedirs")  # allow directory creation
    mock_logger = mocker.patch("strategies.core.file_service.logger")
    mock_to_csv = mocker.patch.object(pd.DataFrame, "to_csv", side_effect=RuntimeError("csv fail"))

    # Act / Assert
    with pytest.raises(RuntimeError):
        FileService.write("1m", sample_df, "file.csv")

    mock_logger.exception.assert_called_once()
    mock_to_csv.assert_called_once()


# ---------------------------------------------------------------------------
# read()
# ---------------------------------------------------------------------------

def test_read_ExistingFile_ReadsSuccessfully(mocker, mock_load_config, tmp_path):
    # Arrange
    file_path = tmp_path / "test.csv"
    file_path.write_text("a,b\n1,2")

    mock_logger = mocker.patch("strategies.core.file_service.logger")

    # Act
    df = FileService.read("test.csv")

    # Assert
    assert df.iloc[0]["a"] == 1
    assert df.iloc[0]["b"] == 2
    mock_logger.info.assert_called_once()


def test_read_FileDoesNotExist_RaisesFileNotFound(mocker, mock_load_config, tmp_path):
    # Arrange
    mock_logger = mocker.patch("strategies.core.file_service.logger")

    # Act / Assert
    with pytest.raises(FileNotFoundError):
        FileService.read("missing.csv")

    mock_logger.exception.assert_called_once()


def test_read_ReadCsvFails_ExceptionLoggedAndReraised(mocker, mock_load_config, tmp_path):
    # Arrange
    file_path = tmp_path / "broken.csv"
    file_path.write_text("dummy")

    mocker.patch("os.path.exists", return_value=True)
    mock_logger = mocker.patch("strategies.core.file_service.logger")
    mock_pandas = mocker.patch("pandas.read_csv", side_effect=RuntimeError("read fail"))

    # Act / Assert
    with pytest.raises(RuntimeError):
        FileService.read("broken.csv")

    mock_logger.exception.assert_called_once()
    mock_pandas.assert_called_once()


# ---------------------------------------------------------------------------
# read_all_csv()
# ---------------------------------------------------------------------------

def test_read_all_csv_ValidDirectory_ReadsAllFiles(mocker, mock_load_config, tmp_path):
    # Arrange
    folder = tmp_path / "1m"
    folder.mkdir()
    (folder / "a.csv").write_text("x,y\n1,2")
    (folder / "b.csv").write_text("x,y\n3,4")

    mocker.patch("os.listdir", return_value=["a.csv", "b.csv"])
    mock_logger = mocker.patch("strategies.core.file_service.logger")
    mock_read = mocker.patch(
        "strategies.core.file_service.FileService.read",
        side_effect=[
            pd.DataFrame({"x": [1], "y": [2]}),
            pd.DataFrame({"x": [3], "y": [4]})
        ]
    )

    # Act
    dfs = FileService.read_all_csv("1m")

    # Assert
    assert len(dfs) == 2
    assert dfs[0].iloc[0]["x"] == 1
    assert dfs[0].iloc[0]["y"] == 2
    assert dfs[1].iloc[0]["x"] == 3
    assert dfs[1].iloc[0]["y"] == 4
    mock_read.assert_called()
    mock_logger.exception.assert_not_called()


def test_read_all_csv_DirectoryMissing_RaisesFileNotFound(mocker, mock_load_config, tmp_path):
    # Arrange
    mock_logger = mocker.patch("strategies.core.file_service.logger")

    # Act / Assert
    with pytest.raises(FileNotFoundError):
        FileService.read_all_csv("not_exist")

    mock_logger.exception.assert_called_once()


def test_read_all_csv_ReadFailsOnOneCsv_ReraisesException(mocker, mock_load_config, tmp_path):
    # Arrange
    folder = tmp_path / "1m"
    folder.mkdir()
    (folder / "bad.csv").write_text("dummy")

    mocker.patch("os.listdir", return_value=["bad.csv"])
    mock_logger = mocker.patch("strategies.core.file_service.logger")
    mocker.patch("strategies.core.file_service.FileService.read", side_effect=RuntimeError("fail"))

    # Act / Assert
    with pytest.raises(RuntimeError):
        FileService.read_all_csv("1m")

    mock_logger.exception.assert_called_once()


# ---------------------------------------------------------------------------
# __get_directory()
# ---------------------------------------------------------------------------

def test_get_directory_ConfigLoaded_ReturnsCorrectPath(mocker):
    # Arrange
    mocker.patch("strategies.core.file_service.load_config", return_value={"data": {"directory": "/datax"}})

    # Act
    result = FileService._FileService__get_directory()

    # Assert
    assert result == "/datax"

