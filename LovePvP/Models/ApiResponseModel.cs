namespace GladiatorHub.Models
{
    public class ApiResponseModel<T>
    {
        public T Data { get; set; }
        public string Message { get; set; }
    }

}
