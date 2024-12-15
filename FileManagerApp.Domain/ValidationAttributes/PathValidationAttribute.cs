using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FileManagerApp.Domain.ValidationAttributes
{
    public class PathValidationAttribute : ValidationAttribute
    {
        private readonly int _maxPathLength;

        // Allow the user to specify the maximum length of the path, defaulting to 1024 characters
        public PathValidationAttribute(int maxPathLength = 1024)
        {
            _maxPathLength = maxPathLength;
        }

        // Method to check if the path is valid
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) 
                return new ValidationResult("Path cannot be null");

            string path = value.ToString();

            // The path cannot be empty
            if (string.IsNullOrEmpty(path))
                return new ValidationResult("Path cannot be empty");

            // The path cannot be longer than the specified length
            if (path.Length > _maxPathLength)
                return new ValidationResult("Path cannot be longer than " + _maxPathLength + " characters");

            // Check  for multiple slashes, standardize to forward slashes
            path = path.Replace("\\", "/");
            if (path.Contains("//"))
                return new ValidationResult("Path cannot contain multiple slashes");

            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c)))
                return new ValidationResult("Path cannot contain any of the following characters");

            return ValidationResult.Success;
        }
    }
}
