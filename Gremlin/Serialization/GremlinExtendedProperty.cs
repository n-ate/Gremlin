namespace n_ate.Gremlin.Serialization
{
    internal class GremlinExtendedProperty<T>
    {
        public string? Id { get; set; }
        public T? Value { get; set; }
    }
}