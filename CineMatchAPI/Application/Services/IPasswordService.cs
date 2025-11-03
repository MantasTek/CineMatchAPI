namespace CineMatchAPI.Application.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Service for securely hashing and verifying passwords using BCrypt.
/// 
/// WHAT WAS FIXED:
/// The original VerifyPassword method would crash (throw an exception) when given an empty
/// or null hash string because BCrypt.Verify() can't parse an empty string as a valid hash format.
/// It's like trying to read a book that has no pages—the reading system crashes because there's
/// nothing to read.
/// 
/// Think of password verification like checking if a key fits a lock:
/// - If you have both a key (password) and a lock (hash), you can test if they match
/// - If you're missing the lock (empty/null hash), you can't test anything, so the answer is "no match"
/// - If the lock is broken/corrupted (malformed hash), you also can't test it, so "no match"
/// 
/// THE SOLUTION:
/// We added "guard clauses" that check for problematic input before trying to use BCrypt:
/// 1. If password OR hash is null/empty → return false immediately
/// 2. Wrap the BCrypt.Verify call in try-catch to handle malformed hashes gracefully
/// 3. Any exception means "couldn't verify" which we treat as "doesn't match" (return false)
/// 
/// This makes the service robust—it handles all edge cases without crashing, which is
/// exactly what the test expects when it passes an empty hash.
/// </summary>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// Hashes a password using BCrypt with automatic salt generation.
    /// BCrypt automatically generates a unique salt for each hash, so the same password
    /// will produce different hashes each time (this is a security feature).
    /// </summary>
    /// <param name="password">The plain-text password to hash</param>
    /// <returns>The BCrypt hash string</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies if a plain-text password matches a BCrypt hash.
    /// This is safe to use even with invalid inputs—it will return false rather than crash.
    /// </summary>
    /// <param name="password">The plain-text password to verify</param>
    /// <param name="hash">The BCrypt hash to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    public bool VerifyPassword(string password, string hash)
    {
        // GUARD CLAUSE: Check for null or empty inputs
        // If either input is missing, we can't verify anything, so return false
        // This prevents BCrypt from throwing an exception on invalid input
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        // SAFE VERIFICATION: Wrap in try-catch to handle malformed hashes
        // BCrypt.Verify expects a properly formatted BCrypt hash. If the hash is
        // corrupted, truncated, or in the wrong format, it will throw an exception.
        // Rather than letting that exception crash our application, we catch it
        // and treat it as "password doesn't match" (which is technically true—
        // if we can't verify the hash, we can't confirm the password is correct)
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            // If BCrypt throws any exception (usually due to invalid hash format),
            // we treat it as a failed verification. This is the safe and correct
            // behavior: when in doubt, deny access.
            return false;
        }
    }
}