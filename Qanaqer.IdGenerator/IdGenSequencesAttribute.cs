using System;

namespace Qanaqer.IdGenerator
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class IdGenSequencesAttribute : Attribute
    {
        /// <summary>
        /// Schema name to use for sequence generation
        /// If not provided default db context schema will be used
        /// </summary>
        public string? SchemaName { get; set; }
    }
}
