using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Domain.ValidationAttributes
{
    public class FileNameValidationAttribute : ValidationAttribute
    {
        // Method to check if the file name is valid
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("File name cannot be null");

            string fileName = value.ToString();

            // The file name cannot be empty
            if (string.IsNullOrEmpty(fileName))
                return new ValidationResult("File name cannot be empty");

            // The file name cannot be longer than 255 characters
            if (fileName.Length > 255)
                return new ValidationResult("File name cannot be longer than 255 characters");

            // The file name cannot contain any of the following characters: \ / : * ? " < > |
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.Any(c => invalidChars.Contains(c)))
                return new ValidationResult("File name cannot contain any of the following characters");

            // Do not allow names that starts or ends with a space
            if(fileName != fileName.Trim())
                return new ValidationResult("File name cannot start or end with a space");

            return ValidationResult.Success;
        }
    }
}
