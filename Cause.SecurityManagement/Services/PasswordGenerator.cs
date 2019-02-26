using System.Security.Cryptography;
using System.Text;

namespace Cause.SecurityManagement.Services
{
	public class PasswordGenerator
	{
		public string EncodePassword(string password, string applicationName)
		{
			var secret = Encoding.UTF8.GetBytes(applicationName);
			var bytePassword = Encoding.UTF8.GetBytes(password);

			var hmac = new HMACSHA256(secret);

			var hmacPassword = hmac.ComputeHash(bytePassword);
			return ByteToString(hmacPassword);
		}

		private string ByteToString(byte[] buff)
		{
			string sbinary = "";

			for (int i = 0; i < buff.Length; i++)
				sbinary += buff[i].ToString("X2"); // hex format

			return sbinary;
		}
	}
}
