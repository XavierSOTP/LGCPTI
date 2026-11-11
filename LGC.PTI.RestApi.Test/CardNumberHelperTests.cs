namespace LGC.PTI.Domain.Infrastructure.Models.Tests
{
    public sealed class CardNumberHelperTests
    {
        [Test]
        public void TryGetCardNumber_ProvidesCardNumber_WhenExtactingNumbersFromStringScenario1()
        {
            var input = "qsl_3000";
            var cardNumber = CardNumberHelper.TryGetCardNumber(input);
            Assert.AreEqual(cardNumber, 3000);
        }

        [Test]
        public void TryGetCardNumber_ProvidesCardNumber_WhenExtactingNumbersFromStringScenario2()
        {
            var input = "qsl-3000";
            var cardNumber = CardNumberHelper.TryGetCardNumber(input);
            Assert.AreEqual(cardNumber, 3000);
        }


      

            [Test]
        public void TryGetCardNumber_ProvidesCardNumberDefaultValue_WhenExtactingNumbersFromStringScenario3()
        {
            var input = "qsl3000";
            var cardNumber = CardNumberHelper.TryGetCardNumber(input);
            Assert.AreEqual(cardNumber, 0000);
        }

        [Test]
        public void TryGetCardNumber_ProvidesCardNumberDefaultValue_WhenExtactingNumbersFromStringWithNoDigits()
        {
            var input = "qslNoDigits";
            var cardNumber = CardNumberHelper.TryGetCardNumber(input);
            Assert.AreEqual(cardNumber, 0000);
        }
    }
}