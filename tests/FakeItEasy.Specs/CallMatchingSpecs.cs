namespace FakeItEasy.Specs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.InteropServices;
    using FakeItEasy.Tests.TestHelpers;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    public static class CallMatchingSpecs
    {
        public interface ITypeWithParameterArray
        {
            void MethodWithParameterArray(string arg, params string[] args);
        }

        public interface IHaveNoGenericParameters
        {
            void Bar(int baz);
        }

        public interface IHaveOneGenericParameter
        {
            void Bar<T>(T baz);
        }

        public interface IHaveTwoGenericParameters
        {
            void Bar<T1, T2>(T1 baz1, T2 baz2);
        }

        public interface IHaveARefParameter
        {
            bool CheckYourReferences(ref string refString);
        }

        public interface IHaveAnOutParameter
        {
            bool Validate([Out] string value);
        }

        public interface IHaveANullableParameter
        {
            int Bar(int? x);
        }

        [Scenario]
        public static void ParameterArrays(
            ITypeWithParameterArray fake)
        {
            "Given a fake"
                .x(() => fake = A.Fake<ITypeWithParameterArray>());

            "When a call with a parameter array is made on this fake"
                .x(() => fake.MethodWithParameterArray("foo", "bar", "baz"));

            "Then an assertion with all the same argument values should succeed"
                .x(() => A.CallTo(() => fake.MethodWithParameterArray("foo", "bar", "baz")).MustHaveHappened());

            "And an assertion with only argument constraints should succeed"
                .x(() => A.CallTo(() => fake.MethodWithParameterArray(A<string>._, A<string>._, A<string>._)).MustHaveHappened());

            "And an assertion with mixed argument values and argument constraints should succeed"
                .x(() => A.CallTo(() => fake.MethodWithParameterArray(A<string>._, "bar", A<string>._)).MustHaveHappened());

            "And an assertion using the array syntax should succeed"
                .x(() => A.CallTo(() => fake.MethodWithParameterArray("foo", A<string[]>.That.IsSameSequenceAs(new[] { "bar", "baz" }))).MustHaveHappened());
        }

        [Scenario]
        public static void FailingMatchOfNonGenericCalls(
            IHaveNoGenericParameters fake,
            Exception exception)
        {
            "Given a fake"
                .x(() => fake = A.Fake<IHaveNoGenericParameters>());

            "And a call with argument 1 made on this fake"
                .x(() => fake.Bar(1));

            "And a call with argument 2 made on this fake"
                .x(() => fake.Bar(2));

            "When I assert that a call with argument 3 has happened on this fake"
                .x(() => exception = Record.Exception(() => A.CallTo(() => fake.Bar(3)).MustHaveHappened()));

            "Then the assertion should fail"
                .x(() => exception.Should().BeAnExceptionOfType<ExpectationException>());

            "And the exception message should tell us that the call was not matched"
                .x(() => exception.Message.Should().Be(@"

  Assertion failed for the following call:
    FakeItEasy.Specs.CallMatchingSpecs+IHaveNoGenericParameters.Bar(3)
  Expected to find it at least once but found it #0 times among the calls:
    1: FakeItEasy.Specs.CallMatchingSpecs+IHaveNoGenericParameters.Bar(baz: 1)
    2: FakeItEasy.Specs.CallMatchingSpecs+IHaveNoGenericParameters.Bar(baz: 2)

"));
        }

        [Scenario]
        public static void FailingMatchOfGenericCalls(
            IHaveTwoGenericParameters fake,
            Exception exception)
        {
            "Given a fake"
                .x(() => fake = A.Fake<IHaveTwoGenericParameters>());

            "And a call with arguments of type int and double made on this fake"
                .x(() => fake.Bar(1, 2D));

            "And a call with arguments of type Generic<bool, long> and int made on this call"
                .x(() => fake.Bar(new Generic<bool, long>(), 3));

            "When I assert that a call with arguments of type string and string has happened on this fake"
                .x(() => exception = Record.Exception(() => A.CallTo(() => fake.Bar(A<string>.Ignored, A<string>.Ignored)).MustHaveHappened()));

            "Then the assertion should fail"
                .x(() => exception.Should().BeAnExceptionOfType<ExpectationException>());

            "And the exception message should tell us that the call was not matched"
                .x(() => exception.Message.Should().Be(@"

  Assertion failed for the following call:
    FakeItEasy.Specs.CallMatchingSpecs+IHaveTwoGenericParameters.Bar<System.String, System.String>(<Ignored>, <Ignored>)
  Expected to find it at least once but found it #0 times among the calls:
    1: FakeItEasy.Specs.CallMatchingSpecs+IHaveTwoGenericParameters.Bar<System.Int32, System.Double>(baz1: 1, baz2: 2)
    2: FakeItEasy.Specs.CallMatchingSpecs+IHaveTwoGenericParameters.Bar<FakeItEasy.Specs.CallMatchingSpecs+Generic<System.Boolean, System.Int64>, System.Int32>(baz1: FakeItEasy.Specs.CallMatchingSpecs+Generic`2[System.Boolean,System.Int64], baz2: 3)

"));
        }

        [Scenario]
        public static void NoNonGenericCalls(
            IHaveNoGenericParameters fake,
            Exception exception)
        {
            "Given a fake"
                .x(() => fake = A.Fake<IHaveNoGenericParameters>());

            "And no calls made on this fake"
                .x(() => { });

            "When I assert that a call has happened on this fake"
                .x(() => exception = Record.Exception(() => A.CallTo(() => fake.Bar(A<int>.Ignored)).MustHaveHappened()));

            "Then the assertion should fail"
                .x(() => exception.Should().BeAnExceptionOfType<ExpectationException>());

            "And the exception message should tell us that the call was not matched"
                .x(() => exception.Message.Should().Be(@"

  Assertion failed for the following call:
    FakeItEasy.Specs.CallMatchingSpecs+IHaveNoGenericParameters.Bar(<Ignored>)
  Expected to find it at least once but no calls were made to the fake object.

"));
        }

        [Scenario]
        public static void NoGenericCalls(
            IHaveOneGenericParameter fake,
            Exception exception)
        {
            "Given a fake"
                .x(() => fake = A.Fake<IHaveOneGenericParameter>());

            "And no calls made on this fake"
                .x(() => { });

            "When I assert that a call has happened on this fake"
                .x(() => exception = Record.Exception(() => A.CallTo(() => fake.Bar<Generic<string>>(A<Generic<string>>.Ignored)).MustHaveHappened()));

            "Then the assertion should fail"
                .x(() => exception.Should().BeAnExceptionOfType<ExpectationException>());

            "And the exception message should tell us that the call was not matched"
                .x(() => exception.Message.Should().Be(@"

  Assertion failed for the following call:
    FakeItEasy.Specs.CallMatchingSpecs+IHaveOneGenericParameter.Bar<FakeItEasy.Specs.CallMatchingSpecs+Generic<System.String>>(<Ignored>)
  Expected to find it at least once but no calls were made to the fake object.

"));
        }

        [Scenario]
        public static void OutParameter(
            IDictionary<string, string> subject,
            string constraintValue,
            string value)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IDictionary<string, string>>());

            "And a call to a method with an out parameter configured on this fake"
                .x(() =>
                    {
                        constraintValue = "a constraint string";
                        A.CallTo(() => subject.TryGetValue("any key", out constraintValue))
                            .Returns(true);
                    });

            "When I make a call to the configured method"
                .x(() => subject.TryGetValue("any key", out value));

            "Then it should assign the constraint value to the out parameter"
                .x(() => value.Should().Be(constraintValue));
        }

        [Scenario]
        public static void FailingMatchOfOutParameter(
            IDictionary<string, string> subject,
            string value,
            Exception exception)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IDictionary<string, string>>());

            "And no calls made on this fake"
                .x(() => { });

            "When I assert that a call with an out parameter happened on this fake"
                .x(() => exception =
                    Record.Exception(
                        () => A.CallTo(() => subject.TryGetValue("any key", out value)).MustHaveHappened()));

            "Then the assertion should fail"
                .x(() => exception.Should().BeAnExceptionOfType<ExpectationException>());

            "And the exception message should tell us that the call was not matched"
                .x(() => exception.Message.Should().Match(@"

  Assertion failed for the following call:
    System.Collections.Generic.IDictionary`2[[System.String, *, Version=4.0.0.0, Culture=neutral, PublicKeyToken=*],[System.String, *, Version=4.0.0.0, Culture=neutral, PublicKeyToken=*]].TryGetValue(""any key"", <out parameter>)
  Expected to find it at least once but no calls were made to the fake object.

"));
        }

        [Scenario]
        public static void RefParameter(
            IHaveARefParameter subject,
            string constraintValue,
            string value,
            bool result)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IHaveARefParameter>());

            "And a call to a method with a ref parameter configured on this fake"
                .x(() =>
                    {
                        constraintValue = "a constraint string";
                        A.CallTo(() => subject.CheckYourReferences(ref constraintValue)).Returns(true);
                    });

            "When I make a call to the configured method with the constraint string"
                .x(() =>
                    {
                        value = constraintValue;
                        result = subject.CheckYourReferences(ref value);
                    });

            "Then it should return the configured value"
                .x(() => result.Should().BeTrue());

            "And it should assign the constraint value to the ref parameter"
                .x(() => value.Should().Be(constraintValue));
        }

        [Scenario]
        public static void FailingMatchOfRefParameter(
            IHaveARefParameter subject,
            string constraintValue,
            string value,
            bool result)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IHaveARefParameter>());

            "And a call to a method with a ref parameter configured on this fake"
                .x(() =>
                {
                    constraintValue = "a constraint string";
                    A.CallTo(() => subject.CheckYourReferences(ref constraintValue)).Returns(true);
                });

            "When I make a call to the configured method with a different value"
                .x(() =>
                {
                    value = "a different string";
                    result = subject.CheckYourReferences(ref value);
                });

            "Then it should return the default value"
                .x(() => result.Should().BeFalse());

            "And it should leave the ref parameter unchanged"
                .x(() => value.Should().Be("a different string"));
        }

        /// <summary>
        /// <see cref="OutAttribute"/> can be applied to parameters that are not
        /// <c>out</c> parameters.
        /// One example is the array parameter in <see cref="System.IO.Stream.Read"/>.
        /// Ensure that such parameters are not confused with <c>out</c> parameters.
        /// </summary>
        /// <param name="subject">The subject of the test.</param>
        /// <param name="result">The result of the call.</param>
        [Scenario]
        public static void ParameterHavingAnOutAttribute(
             IHaveAnOutParameter subject,
             bool result)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IHaveAnOutParameter>());

            "And a call to a method with a parameter having an out attribute is configured on this fake"
                .x(() => A.CallTo(() => subject.Validate("a constraint string")).Returns(true));

            "When I make a call to the configured method with the constraint string"
                .x(() => result = subject.Validate("a constraint string"));

            "Then it should return the configured value"
                .x(() => result.Should().BeTrue());
        }

        /// <summary>
        /// <see cref="OutAttribute"/> can be applied to parameters that are not
        /// <c>out</c> parameters.
        /// One example is the array parameter in <see cref="System.IO.Stream.Read"/>.
        /// Ensure that such parameters are not confused with <c>out</c> parameters.
        /// </summary>
        /// <param name="subject">The subject of the test.</param>
        /// <param name="result">The result of the call.</param>
        [Scenario]
        public static void FailingMatchOfParameterHavingAnOutAttribute(
             IHaveAnOutParameter subject,
             bool result)
        {
            "Given a fake"
                .x(() => subject = A.Fake<IHaveAnOutParameter>());

            "And a call to a method with a parameter having an out attribute is configured on this fake"
                .x(() => A.CallTo(() => subject.Validate("a constraint string")).Returns(true));

            "When I make a call to the configured method with a different string"
                .x(() => result = subject.Validate("a different string"));

            "Then it should return the default value"
                .x(() => result.Should().BeFalse());
        }

        [Scenario]
        public static void ThatArgumentConstraintForValueTypeWithNullArgument(
            IHaveANullableParameter subject,
            int result)
        {
            "Given a fake with a method that accepts a nullable value type parameter"
                .x(() => subject = A.Fake<IHaveANullableParameter>());

            "And a call configured for a non-nullable argument of that type"
                .x(() => A.CallTo(() => subject.Bar(A<int>._)).Returns(42));

            "When I make a call to this method with a null argument"
                .x(() => result = subject.Bar(null));

            "Then it doesn't match the configured call"
                .x(() => result.Should().Be(0));
        }

        [Scenario]
        public static void IgnoredArgumentConstraintOutsideCallSpec(
            Exception exception)
        {
            "When A<T>.Ignored is used outside a call specification"
                .x(() => exception = Record.Exception(() => A<string>.Ignored));

            "Then it should throw an InvalidOperationException"
                .x(() => exception.Should().BeAnExceptionOfType<InvalidOperationException>());

            "And the exception message should explain why it's invalid"
                .x(() => exception.Message.Should().Be("A<T>.Ignored, A<T>._, and A<T>.That can only be used in the context of a call specification with A.CallTo()"));
        }

        [Scenario]
        public static void ThatArgumentConstraintOutsideCallSpec(
            Exception exception)
        {
            "When A<T>.That is used outside a call specification"
                .x(() => exception = Record.Exception(() => A<string>.That.Not.IsNullOrEmpty()));

            "Then it should throw an InvalidOperationException"
                .x(() => exception.Should().BeAnExceptionOfType<InvalidOperationException>());

            "And the exception message should explain why it's invalid"
                .x(() => exception.Message.Should().Be("A<T>.Ignored, A<T>._, and A<T>.That can only be used in the context of a call specification with A.CallTo()"));
        }

        [Scenario]
        [MemberData("Fakes")]
        public static void PassingAFakeToAMethod<T>(
            T fake, string fakeDescription, IHaveOneGenericParameter anotherFake, Exception exception)
        {
            $"Given a fake {fakeDescription}"
                .x(() => { });

            "And another fake"
                .x(() => anotherFake = A.Fake<IHaveOneGenericParameter>());

            "When I assert that a call to the second fake must have happened with the first fake as an argument"
                .x(() => exception = Record.Exception(() => A.CallTo(() => anotherFake.Bar(fake)).MustHaveHappened()));

            "Then the call should be described in terms of the first fake"
                .x(() => exception.Message.Should().Contain(
                    $"FakeItEasy.Specs.CallMatchingSpecs+IHaveOneGenericParameter.Bar<").And.Subject.Should().Contain($">({fakeDescription})"));
        }

        [Scenario]
        [MemberData("FakesWithBadToString")]
        public static void PassingAFakeWithABadToStringToAMethod<T>(
            T fake, string fakeDescription, IHaveOneGenericParameter anotherFake, Exception exception)
        {
            $"Given a fake {fakeDescription} with a ToString method which throws"
                .x(() => A.CallTo(() => fake.ToString()).Throws<Exception>());

            "And another fake"
                .x(() => anotherFake = A.Fake<IHaveOneGenericParameter>());

            "When I assert that a call to the second fake must have happened with the first fake as an argument"
                .x(() => exception = Record.Exception(() => A.CallTo(() => anotherFake.Bar(fake)).MustHaveHappened()));

            "Then the call should be described in terms of the first fake"
                .x(() => exception.Message.Should().Contain(
                    $"FakeItEasy.Specs.CallMatchingSpecs+IHaveOneGenericParameter.Bar<").And.Subject.Should().Contain($">({fakeDescription})"));
        }

        [Scenario]
        public static void PassingAnObjectWithABadToStringToAMethod(
            ToStringThrows obj, IHaveOneGenericParameter fake, Exception exception)
        {
            $"Given an object with a ToString method which throws"
                .x(() => obj = new ToStringThrows());

            "And a fake"
                .x(() => fake = A.Fake<IHaveOneGenericParameter>());

            "When I assert that a call to the fake must have happened with the object as an argument"
                .x(() => exception = Record.Exception(() => A.CallTo(() => fake.Bar(obj)).MustHaveHappened()));

            "Then the call should be described in terms of the object"
                .x(() => exception.Message.Should().Contain(
                    $"FakeItEasy.Specs.CallMatchingSpecs+IHaveOneGenericParameter.Bar<").And.Subject.Should().Contain($">({obj.GetType().ToString()})"));
        }

        [Scenario]
        [MemberData("StrictFakes")]
        public static void PassingAStrictFakeToAMethod<T>(
            Func<T> createFake, string fakeDescription, T fake, IHaveOneGenericParameter anotherFake, Exception exception)
        {
            $"Given a strict fake {fakeDescription}"
                .x(() => fake = createFake());

            "And another fake"
                .x(() => anotherFake = A.Fake<IHaveOneGenericParameter>());

            "When I assert that a call to the second fake must have happened with the first fake as an argument"
                .x(() => exception = Record.Exception(() => A.CallTo(() => anotherFake.Bar(fake)).MustHaveHappened()));

            "Then the call should be described in terms of the first fake"
                .x(() => exception.Message.Should().Contain(
                    $"FakeItEasy.Specs.CallMatchingSpecs+IHaveOneGenericParameter.Bar<").And.Subject.Should().Contain($">({fakeDescription})"));
        }

        public static IEnumerable<object[]> Fakes()
        {
            yield return new object[] { A.Fake<object>(), "Faked " + typeof(object).FullName };
            yield return new object[] { A.Fake<object>(), "Faked " + typeof(object).ToString() };
            yield return new object[] { A.Fake<List<int>>(), "Faked " + typeof(List<int>).FullName };
            yield return new object[] { A.Fake<IList<int>>(), "Faked " + typeof(IList<int>).FullName };
            yield return new object[] { A.Fake<Action<int>>(), typeof(Action<int>).ToString() };
        }

        public static IEnumerable<object[]> FakesWithBadToString()
        {
            yield return new object[] { A.Fake<object>(), "Faked " + typeof(object).FullName };
            yield return new object[] { A.Fake<object>(), "Faked " + typeof(object).ToString() };
            yield return new object[] { A.Fake<List<int>>(), "Faked " + typeof(List<int>).FullName };
            yield return new object[] { A.Fake<IList<int>>(), "Faked " + typeof(IList<int>).FullName };
        }

        public static IEnumerable<object[]> StrictFakes()
        {
            yield return new object[] { new Func<object>(() => A.Fake<object>(o => o.Strict())), "Faked " + typeof(object).FullName };
            yield return new object[] { new Func<object>(() => A.Fake<object>(o => o.Strict())), "Faked " + typeof(object).ToString() };
            yield return new object[] { new Func<object>(() => A.Fake<List<int>>(o => o.Strict())), "Faked " + typeof(List<int>).FullName };
            yield return new object[] { new Func<object>(() => A.Fake<IList<int>>(o => o.Strict())), "Faked " + typeof(IList<int>).FullName };
            yield return new object[] { new Func<object>(() => A.Fake<Action<int>>(o => o.Strict())), typeof(Action<int>).ToString() };
        }

        public class Generic<T>
        {
        }

        public class Generic<T1, T2>
        {
        }

        public class ToStringThrows
        {
            public override string ToString()
            {
                throw new Exception();
            }
        }
    }
}
