namespace EnTouch.Application.DTOs
{
    public class DictionarySignDto
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
        public string Language { get; set; }
        public string VideoPath { get; set; }
        public string? Description { get; set; }
    }
}