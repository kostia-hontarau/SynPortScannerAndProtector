namespace ConsoleApplication1.Model.Validation
{
    internal interface IValidator<T>
    {
        bool Validate(T obj);
    }
}
