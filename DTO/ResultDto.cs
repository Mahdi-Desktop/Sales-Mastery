namespace AspnetCoreMvcFull.DTO
{
  public class ResultDto
  {
    public bool Success { get; set; }
    public string Message { get; set; }

    public string User { get; set; }
    public object Data { get; set; }
  }
}
