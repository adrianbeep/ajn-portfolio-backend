using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string PasswordHash { get; private set; }
        public string Email { get; private set; } = string.Empty;

        // Constructor para inicialización del ORM
        protected User() { }

        // Constructor para la creación del usuario
        public User(string passwordHash, string email)
        {
            ValidatePasswordHash(passwordHash);
            ValidateEmail(email);

            Id = Guid.NewGuid();
            PasswordHash = passwordHash;
            Email = email.Trim().ToLowerInvariant();
        }

        private void ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentNullException("Password hash cannot be empty.", nameof(passwordHash));
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException("Email cannot be empty.", nameof(email));

            if (email.Trim().Length > 100)
                throw new ArgumentException("Email cannot exceed 100 characters.", nameof(email));

            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, emailPattern))
            {
                throw new ArgumentException("Invalid email format.", nameof(email));
            }
        }
    }
}
