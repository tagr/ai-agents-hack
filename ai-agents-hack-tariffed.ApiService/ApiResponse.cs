namespace ai_agents_hack_tariffed.ApiService
{
    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string ThreadId { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
        public ApiResponse(string message)
        {
            Message = message;
        }
        public ApiResponse()
        {
            
        }
    }
}
