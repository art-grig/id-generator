namespace Qanaqer.IdGenerator
{
    public class SequenceConfig
    {
        public static readonly SequenceConfig Default = new SequenceConfig();

        public int IncrementBy { get; set; } = 1;
        public int StartWith { get; set; } = 1;
        public string? SchemaName { get; set; }
    }
}
