namespace TestProject
{
    class Program
    {
        static void Main()
        {
            var derivedOne = new DerivedOne();
            var derivedTwo = new DerivedTwo();

            var one = derivedOne.Create();
            var two = derivedTwo.Create();
        }
    }

    public class Base<TSelf> where TSelf : Base<TSelf>, new()
    {
        public TSelf Create() => new TSelf();
    }

    public class DerivedOne : Base<DerivedOne>
    {
        void SomeMethod()
        {
            var someMethodCall = Create();
        }
    }

    public class DerivedTwo : Base<DerivedTwo> { }
}
