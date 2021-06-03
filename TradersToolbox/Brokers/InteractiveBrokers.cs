#if ANASJDHWLKD

using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Deployment.Internal.CodeSigning;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TradersToolbox.Brokers
{
    class InteractiveBrokers : IBrokerService
    {
        const string RequestTokenURL = "https://www.interactivebrokers.com/tradingapi/v1/oauth/request_token";
        const string CallbackURL = "https://dev.buildalpha.com/auth/callback";
        const string privateSignatureKeyPemStr = /*@"-----BEGIN RSA PRIVATE KEY-----
MIIEpQIBAAKCAQEAxH9rf1RAaLNGrZh2Bo9dfzy9qLsR4VpqECArG/KWnZ3LhToR
cjRpP6Wa/LLXBR5Ub2wG3EXhVNlzbjwPReDi6SUQLeIVkUeAwRfZRbg2N2NTSohA
0JMNo1N3hEjNbfBpCM99MyldH+rO5+ixMWdBXfyn2uwtrtAT8VbxgKTZ9qbjJdBF
u0Em2xncQDEfH++TcDFrKoBVSVBYBGYnhtslEK1G/Cxllzb/weEdW+LCbFCybJm3
0sfjN3d/hWUNnYR0t8JR29sI28Ub6uBUEaMc1run5kKWJIhe7fGaPT84puIZpxFZ
Pbr6/8ewIZAzrNI6eYE3HoDy6pw/0JUMzokzrQIDAQABAoIBAQCs0UATCV9odhv4
O3m6RUa+zAYmKvkL0MAtlI01lELBNvGzJ6IGZnAWiSjpjMStfMJYCJN0EYWfYWwz
DGGGXMZUtMMpB4leb5uet8HgnqYYSqQLjhArINpCxfr8ficmxOUr2YjMgVmk45e7
wRxuTdjgt4BNe6Cl+d1ASe427UXBtfFvj0wR739WQuB/VpZ1s+KFSG7fRPJL5pKe
Ti++7nAfTKKXE6HJo63YNDmu+SaFQBtPsEoQYlzzW9BXSzepN9zklB89YrQdrayJ
l4Z25Rq+zSMGsrR41hY463f44jTHzhMA5xn5De3Apdc/PKpUPdfovNAlZbGnKkd8
ewV52uLhAoGBAP+9TNl0yitIX9QBgNvfHGOalZckfp8DOeeDJnFf+JxTE2f8m9md
luGoEEL+9M6MQhaqi66VTtTcYUhSkukps0uDSIRmdxsQHxU/vnULBX5ehPGtDStN
9UJ/zU+DgmSN0fqk68senwvzxzUaAwiXtuV+234D9Kgeo+LYjxv8d96JAoGBAMSy
qzYDX9UtkUvh8AeBds7z3S3II2XV6S3NzWbrqpkzpq5BOvTZfDKRSANG0h2vZNYg
2INwV9v6Un3f59fsrNM3gBaIATGJ/wODjewXNxuZPsYWyeleLZoMISb71ZPjU6Cc
EHIQc+tkudA3RmsKIHPHIwVd3z1/7bo7MFWcVEMFAoGBALacqJx6jcYcVvke3bGi
/jWNc5XKZHQNLLLI7pF0cyTFGY1eTrc0lUGq7Xm5bntyodpni3htey3586ld9TNz
KGkaZJTOSexUNa3Jp6kR5qlWut2LsWSjtSb39VX61QgSUMcGLq0Kv40cVLnxSgs/
skTrsbWpA3Fs95+K/Z2BSOLpAoGAKvhROMqB7AM5Y5dnJWyd9NYHcpHvUbbqaswz
3BENwswq1JSBea3tWOquDbEyA7QmVjT0t7oaJ39G+CKq9lvI4ZeVTtbFU/oukKjz
nyjvLANNWF7wGyAs3CNcNrT7UoRt539QhGqduEXX5em9Y3Lh5gkR2IFKXJgO6gk2
JO2y5rUCgYEA1Srrah1N7IHdw5cfB/20oERbEiB33U/BVD4RsZ677G78+uhQlzbB
zV5YfJ7RenPQAkN2v8UuDSfIhz2II3Q04Dq7O8JmHBQy7ev5JmaHd3w60tk6JidG
x/3HmjmksvfZGAtOVeVHgF2bLE8uEfUJb110nZ8GqxQ+UVmpas8v5OY=
-----END RSA PRIVATE KEY-----";     //PKCS#1*/
            @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAw4KM7Id3u65efSSPjj2FycnNFZX7ZpuObU8V0Oj5c2LEIn7j
gMHytG3Nhz7zJVR/7MWBFBkeh+Ctw/Y//7hW5unYNf+GBU4OvLZ6EXlJOsf5M/IB
f/Fige3FcrEkL4Vjyvsxsg16uMOyeFzKVfYEn3riKpF3IQsL5E1W8rBgegu48Mkn
ok0S/14UHzY+ohpy6LOtFM+yLmNM7ryRrySHYC05yOtTAms+fQ814ZNAE8mUdNIX
4Br74zcQTTlFb04fMKtfDVnq9tO3IXvImFxdJB0xDvI2m1r/ZP+jpfjQVtmJyg91
nBHHWv7IovggB/mJl1XKDnbQo/aCSqfer6FEKwIDAQABAoIBAAGJThgxvFQiO6Xa
GrKwcxOx3UW1JwOYZ1cejvpUSlmIxifLPXhujbyzPDE64mhBsLuolaoVxHKm5laq
4bfdt/kaj1NlTaVPBrie6nimrlei6Kdq2G4r9G029B1BnRoufylCLhLLHlLDXuyp
NgHU7BZgvdQ9zmTZFF/X574Ces+asnNUr2Rm61IibjjM1xc+4NP2B99R9Ziu9jyK
Raea4WtXY1s4tD3Pfb9Oh8FESCQMv2ge2mgfaF1in9McPePjxslFFG3xSdJ95oMe
z0HDhGnm7tCbBiwW9vAFG7oIY4w/QPanCIyVG3cFqujGGNSWX78plbhLqwGgi6M3
RFm9H9kCgYEA8mgHc5WS9Ugjb/SYPrAZvLtX5B9aUVrWeokGSxOOh+p8Lzi+GTJt
lWv+kiuiZ+74pCx614Yszmpybr4Co4GCogJJ41/DRoE3zm4IfF/dsRnIepvYqyyy
aDx0gXwHoK9QK6P8/KybqnXAc8oh8+8aH2WZK/wdVixPegP4PbVcUy0CgYEAznlH
jrmPAdomFWNBGUEOGK4spP59X4M90yRXPDriiYoq+lp2EUrnguQwbCNIIQ0C6uFQ
hvMgJCFJP8PfJRg2hhsPnPjJ9ZgLwG0ze6f0ZL+YdRBCapFi/BMlW0I98jJSlV89
HydOUoQ+3NRcFeKWrImOcmMulCtS5Jq9w0YWa7cCgYAJvSY3fZXm5twNnm0Tb63J
CFnSn3PYeubNC35GO+XpDgGpQAVbK8x9SVZz9u7ScCZrKiJRUGGydVJdoqKmgQiH
i8H+MZW92mkskP0kShG1EM0eJ+6/ic8tIuinXx1LVl/JMRBz8ldatNpjjIZqr3uE
MWfC/aEMGkGjLE+n4wZvjQKBgGOC7EwLfrMj0qsINT3YrtSSTY4P4ZkBBfEXWLv8
nWYwo1oZ80GOWlopZHUZ5A+Z18ggY9FGqD0Ble4Xlxqnus+Th0jeV2f4qeFKQD/e
yNktxmrVNU1rWMuo5p4/JN/wBQFc73ZuWT9H4YxJBUC+/mOVwIO87ZwX7VGkisTs
qK2tAoGAAgw7iCYye18puPYiMyXLPsOCki6Q3b17eclbiaDoTHi4LANCUHalGwax
KOrU83QzHlFrhMKnwuJWuZskKI5M8o2Uha54ZPXnAWMTcKAzw1S+/7L4i90hFrvO
4EX8iFvQLVGYK9oom8i/SlzXEXOQ9ZGokI9Q2yeVzrEcnFr4YV0=
-----END RSA PRIVATE KEY-----";
        const string consumerKey = "TESTCONS";

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public bool IsLoggedIn { get; set; }

        public async Task<bool> LogIn(HttpClient httpClient)
        {
            string oauth_nonce = /*"fcbc9c08d69ac269f7f1";*/ Guid.NewGuid().ToString().Replace("-", "").Substring(0,20);
            string oauth_timestamp = /*"1473793701";*/ Security.ToUnixTime(DateTime.Now.ToUniversalTime()).ToString();
            string oauthString = "POST&" + Uri.EscapeDataString(RequestTokenURL) +   /*"http%3A%2F%2Flocalhost%3A12345%2Ftradingapi%2Fv1%2Foauth%2Frequest_token" +*/
                $"&oauth_callback%3Doob%26oauth_consumer_key%3D{consumerKey}%26oauth_nonce%3D{oauth_nonce}%26oauth_signature_method%3DRSA-SHA256%26oauth_timestamp%3D{oauth_timestamp}";

            // Sign oauth string with RSA-SHA256
            string oauth_signature;
            try
            {
                PemReader pemReader = new PemReader(new StringReader(privateSignatureKeyPemStr));
                AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                RsaKeyParameters privateKey = (RsaKeyParameters)keyPair.Private;
                //RsaPrivateCrtKeyParameters privateKey = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();

                byte[] inputBytes = Encoding.UTF8.GetBytes(oauthString);

                ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");
                sig.Init(true, privateKey);
                sig.BlockUpdate(inputBytes, 0, inputBytes.Length);
                byte[] signedBytes = sig.GenerateSignature();
                oauth_signature = Convert.ToBase64String(signedBytes);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "RSA-SHA256 signing");
                return false;
            }

            string oauth_request =
                "oauth_callback=\"oob\"," +                         //fixed
                $"oauth_consumer_key=\"{consumerKey}\"," +
                $"oauth_nonce=\"{oauth_nonce}\"," +                 //random alphanumeric string
                $"oauth_signature=\"{Uri.EscapeDataString(oauth_signature)}\"," +
                "oauth_signature_method=\"RSA-SHA256\"," +          //fixed
                $"oauth_timestamp=\"{oauth_timestamp}\"," +
                "realm=\"test_realm\"";     //host

            try
            {
                // Send request
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", oauth_request);
                httpClient.DefaultRequestHeaders.Host = "www.interactivebrokers.com";
                httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Apache-HttpClient/4.5.1 (Java/1.8.0_102)");
                var response = await httpClient.PostAsync(RequestTokenURL, null);

                var errorStr = await response.Content.ReadAsStringAsync();


                /*string AuthorizationCode = e.Url.OriginalString.Substring(1 + e.Url.OriginalString.IndexOf('='));

                auth

                HttpContent x = new StringContent("grant_type=authorization_code&" +
                    $"client_id={TSClientId}&redirect_uri={RedirectUrl}&client_secret={TSClientSecret}&code={AuthorizationCode}&response_type=token",
                    Encoding.UTF8, "application/x-www-form-urlencoded");


                if (response.StatusCode == HttpStatusCode.OK)
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TradeStationAPI.Model.UserCredentials));
                    if (serializer.ReadObject(await response.Content.ReadAsStreamAsync()) is TradeStationAPI.Model.UserCredentials user)
                    {
                        LogLabel.Text += "\nSuccessful";
                        AccessToken = user.access_token;
                        RefreshToken = user.refresh_token;
                        UserId = user.userid;
                        Close();
                    }
                    else
                        throw new Exception("Can't read access token");
                }
                else
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }*/
            }
            catch (Exception ex)
            {
                /*LogLabel.Text += ex.Message;
                webBrowser1.Navigate("about:blank");
                webBrowser1.Refresh();
                webBrowser1.Navigate(TSStartPage);
                webBrowser1.Show();
                MessageBox.Show("Logging in - Unsuccessful\n\n" + ex.Message);*/
            }

            return true;
        }
    }
}
#endif