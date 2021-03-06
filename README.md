# SubTypeReferencesAnalysis

Find usages of a base class method just when called from a specified sub class using Roslyn.

Example:

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

We might want to find all the usages of Create() just when the target of the invocation is DerivedOne, so far neither Visual Studio nor ReSharper provided this functionality so I wrote this tool.