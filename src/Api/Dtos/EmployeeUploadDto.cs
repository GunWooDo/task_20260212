namespace Api.Dtos;

public class EmployeeUploadDto
{
    /// <summary>
    /// 업로드할 CSV 또는 JSON 파일
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// 직접 입력한 데이터 (CSV 또는 JSON 텍스트)
    /// </summary>
    public string? Data { get; set; }
}
