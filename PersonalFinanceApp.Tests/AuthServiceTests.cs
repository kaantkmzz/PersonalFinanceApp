using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.Tests
{
    [TestClass]
    public class AuthServiceTests
    {
        [TestMethod]
        public void IsValidEmailFormat_GecerliEmailKabulEdiliyorMu()
        {
            Assert.IsTrue(AuthService.IsValidEmailFormat("kaan@gmail.com"));
        }

        [TestMethod]
        public void IsValidEmailFormat_AtIsaretiOlmayanEmailReddediliyorMu()
        {
            Assert.IsFalse(AuthService.IsValidEmailFormat("kaangmail.com"));
        }

        [TestMethod]
        public void IsValidEmailFormat_NoktaOlmayanEmailReddediliyorMu()
        {
            Assert.IsFalse(AuthService.IsValidEmailFormat("kaan@gmailcom"));
        }

        [TestMethod]
        public void GetEmailDomainSuggestion_YayginYazimHatasiniYakaliyorMu()
        {
            Assert.AreEqual("gmail.com", AuthService.GetEmailDomainSuggestion("kaan@gmal.com"));
        }

        [TestMethod]
        public void GetEmailDomainSuggestion_DogruDomaindeOneriVermemeliMi()
        {
            Assert.IsNull(AuthService.GetEmailDomainSuggestion("kaan@gmail.com"));
        }
    }
}