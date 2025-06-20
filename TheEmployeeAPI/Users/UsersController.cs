using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
  private readonly UserManager<User> _userManager;
  private readonly SignInManager<User> _signInManager;
  private readonly ILogger<UsersController> _logger;
  private readonly AppDbContext _dbContext;

  public UsersController (
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<UsersController> logger,
    AppDbContext dbContext
  ) {
    _userManager = userManager;
    _signInManager = signInManager;
    _logger = logger;
    _dbContext = dbContext;
  }

  /// <summary>
  /// Register a new user
  /// </summary>
  /// <param name="request">Registration details</param>
  /// <returns>A link to the user that was created</returns>
  [HttpPost("register")]
  [ProducesResponseType(StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
  {
    var user = new User
    {
      UserName = request.Email,
      Email = request.Email,
      FirstName = request.FirstName,
      LastName = request.LastName,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var result = await _userManager.CreateAsync(user, request.Password!);

    if (result.Succeeded) {
      user.LastLoginDate = DateTime.UtcNow;
      await _userManager.UpdateAsync(user);

      await _signInManager.SignInAsync(user, isPersistent: false);

      var authResponse = new AuthResponse {
        IsAuthenticated = true,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        LastLoginDate = user.LastLoginDate,
        Message = "User registered successfully" 
      };

      return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, authResponse);
    }

    return BadRequest(new ErrorResponse
    {
      Message = "Registration failed",
      Errors = result.Errors.Select(e => e.Description)
    });
  }

  /// <summary>
  /// Login a user
  /// </summary>
  /// <param name="request">The user credentials</param>
  /// <returns>The logged User</returns>
  [HttpPost("login")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
  {
    var user = await _userManager.FindByEmailAsync(request.Email!);

    if (user == null) {
      _logger.LogInformation("User not found while login {email}.", request.Email);
      return BadRequest(new ErrorResponse 
      { 
          Message = "Login failed."
      });
    }

    if (!user.IsActive) {
      _logger.LogInformation("User {email} deactivated.", request.Email);
      return BadRequest(new ErrorResponse 
      { 
          Message = "Account is deactivated. Please contact support." 
      });
    }

    var result = await _signInManager.PasswordSignInAsync(
      request.Email!,
      request.Password!,
      request.RememberMe,
      lockoutOnFailure: true
    );

    if (result.Succeeded) {
      user.LastLoginDate = DateTime.UtcNow;

      return new AuthResponse{
        IsAuthenticated = true,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        LastLoginDate = user.LastLoginDate,
        Message = "Login successful" 
      };
    }

    if (result.IsLockedOut)
    {
      _logger.LogInformation("User {email} account locked.", request.Email);
      return BadRequest(new ErrorResponse 
      { 
          Message = "Account locked out. Please try again later." 
      });
    }

    _logger.LogInformation("Invalid email or password while login {email}.", request.Email);
    return BadRequest(new ErrorResponse 
    { 
      Message = "Invalid email or password" 
    });
  }

  /// <summary>
  /// Get all users with optional filtering and pagination
  /// </summary>
  /// <param name="request">Filtering and pagination parameters</param>
  /// <returns>An array of users.</returns>
  [HttpGet]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<IEnumerable<GetUserResponse>>> GetAllUsers([FromQuery] GetAllUsersRequest? request) 
  {
    int page = request?.Page ?? 1;
    int numberOfRecordsPerPage = request?.RecordsPerPage ?? 10;

    IQueryable<User> query = _dbContext.Users
      .Skip((page - 1) * numberOfRecordsPerPage)
      .Take(30);

    // Filters
    if (request != null) {
      if (!string.IsNullOrWhiteSpace(request!.EmailContains))
      {
        query = query.Where(u => u.Email!.Contains(request.EmailContains));
      }

      if (!string.IsNullOrWhiteSpace(request!.FirstNameContains))
      {
        query = query.Where(u => u.FirstName!.Contains(request.FirstNameContains));
      }

      if (!string.IsNullOrWhiteSpace(request!.LastNameContains))
      {
        query = query.Where(u => u.LastName!.Contains(request.LastNameContains));
      }

      if (request.IsActive.HasValue)
      {
        query = query.Where(u => u.IsActive == request.IsActive.Value);
      }
    }

    var users = await query
      .OrderBy(u => u.Email)
      .ToArrayAsync();

    return Ok(users.Select(UserToGetUserResponse).ToArray());
  }

  /// <summary>
  /// Get a user by ID.
  /// </summary>
  /// <param name="id">The ID of the user.</param>
  /// <returns>The single user record.</returns>
  [HttpGet("{id}")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<GetUserResponse>> GetUserById([FromRoute] string id) {
    var user = await _dbContext.Users.SingleOrDefaultAsync(e => e.Id == id);

    if (user == null) {
      return NotFound();
    }

    return Ok(UserToGetUserResponse(user));
  }

  /// <summary>
  /// Get current user information
  /// </summary>
  /// <returns>Current user details</returns>
  [HttpGet("current")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<GetUserResponse>> GetCurrentUser()
  {
      var user = await _userManager.GetUserAsync(User);
      if (user == null)
      {
          return NotFound();
      }

      return Ok(UserToGetUserResponse(user));
  }

  /// <summary>
  /// Update current user profile
  /// </summary>
  /// <param name="request">Profile update data</param>
  /// <returns>Updated user information</returns>
  [HttpPut("profile")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<GetUserResponse>> UpdateProfile([FromBody] UpdateUserRequest request)
  {
    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
      _logger.LogInformation("Current user with ID not found");
      return NotFound();
    }

    user.FirstName = request.FirstName;
    user.LastName = request.LastName;
    user.ProfilePicture = request.ProfilePicture;
    user.UpdatedAt = DateTime.UtcNow;

    try {
      await _userManager.UpdateAsync(user);
      return Ok(UserToGetUserResponse(user));
    } catch (Exception ex) {
      _logger.LogInformation(ex, "Error occurred while updating current user.");
      return StatusCode(500, "An error occurred while updating the current user.");
    }
  }

  /// <summary>
  /// Update a user by ID
  /// </summary>
  /// <param name="id">The ID of the user to update</param>
  /// <param name="request">User update data</param>
  /// <returns>Updated user information</returns>
  [HttpPut("{id}")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<GetUserResponse>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
  {
    _logger.LogInformation("Updating user with ID: {UserId}", id);

    var existingUser = await _dbContext.Users
        .AsTracking()
        .SingleOrDefaultAsync(u => u.Id == id);

    if (existingUser == null)
    {
        return NotFound();
    }

    existingUser.FirstName = request.FirstName;
    existingUser.LastName = request.LastName;
    existingUser.ProfilePicture = request.ProfilePicture;
    existingUser.UpdatedAt = DateTime.UtcNow;

    try {
      await _dbContext.SaveChangesAsync();
      return Ok(UserToGetUserResponse(existingUser));
    } catch (Exception ex) {
      _logger.LogInformation(ex, "Error occurred while updating the user with ID: {id}", id);
      return StatusCode(500, "An error occurred while updating the user.");
    }
  }

  /// <summary>
  /// Deactivate a user
  /// </summary>
  /// <param name="id">The ID of the user to deactivate</param>
  /// <returns>No content on success</returns>
  [HttpDelete("{id}")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> DeactivateUser(string id)
  {
    var user = await _userManager.FindByIdAsync(id);
    if (user == null)
    {
        return NotFound();
    }

    user.IsActive = false;
    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    _logger.LogInformation("Disable user with ID: {id}", id);
    return Ok();
  }

  /// <summary>
  /// Logout current user
  /// </summary>
  /// <returns>Authentication response</returns>
  [HttpPost("logout")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<AuthResponse>> Logout()
  {
    await _signInManager.SignOutAsync();
    return Ok(new AuthResponse 
    { 
        IsAuthenticated = false,
        Message = "Logout successful" 
    });
  }

  /// <summary>
  /// Check authentication status
  /// </summary>
  /// <returns>Authentication status</returns>
  [HttpGet("status")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public ActionResult<AuthResponse> GetAuthStatus()
  {
    return Ok(new AuthResponse
    { 
        IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
        UserName = User.Identity?.Name,
        Message = "Authentication status retrieved successfully"
    });
  }

  /// <summary>
  /// Change current user password
  /// </summary>
  /// <param name="request">Password change data</param>
  /// <returns>Password change result</returns>
  [HttpPost("change-password")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<PasswordResponse>> ChangePassword(
    [FromBody] ChangePasswordRequest request
  ) {
    var user = await _userManager.GetUserAsync(User);

    if (user == null) {
      return BadRequest(new PasswordResponse {
        Success = false,
        Message = "User not found",
        Errors = ["User not found"]
      });
    }

    var result = await _userManager.ChangePasswordAsync(
      user, 
      request.CurrentPassword!, 
      request.NewPassword!
    );

    if (result.Succeeded) {
      _logger.LogInformation("Password changed successfully for user {Email}", user.Email);
      return Ok(new PasswordResponse {
        Success = true,
        Message = "Password changed successfully"
      });
    }

    _logger.LogInformation("Password change failed for user {Email}", user.Email);
    return BadRequest(new PasswordResponse {
      Success = false,
      Message = "Password change failed",
      Errors = result.Errors.Select(e => e.Description)
    });
  }

  /// <summary>
  /// Send forgot password email
  /// </summary>
  /// <param name="request">Forgot password request</param>
  /// <returns>Result of forgot password request</returns>
  [HttpPost("forgot-password")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<PasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request) {
    var user = await _userManager.FindByEmailAsync(request.Email!);
    
    if (user == null || !user.IsActive) {
      _logger.LogInformation("Forgot password requested for non-existent or inactive user: {Email}", request.Email);
      return Ok(new PasswordResponse {
        Success = true,
        Message = "A password reset link has been sent."
      });
    }

    // TODO: Send token by email to user.
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    
    // Here you would send the email with the token
    // For now, we'll just log it (in production, integrate with email service)
    _logger.LogInformation("Password reset token generated for user {Email}: {Token}", user.Email, token);
    
    return Ok(new PasswordResponse {
      Success = true,
      Message = "A password reset link has been sent."
    });
  }

  /// <summary>
  /// Reset password using token
  /// </summary>
  /// <param name="email">User email</param>
  /// <param name="token">Reset token</param>
  /// <param name="request">New password data</param>
  /// <returns>Password reset result</returns>
  [HttpPost("reset-password")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<PasswordResponse>> ResetPassword(
    [FromQuery] string email,
    [FromQuery] string token,
    [FromBody] ResetPasswordRequest request
  ) {
    var user = await _userManager.FindByEmailAsync(email);
    
    if (user == null || !user.IsActive) {
      return BadRequest(new PasswordResponse {
        Success = false,
        Message = "Invalid request",
        Errors = ["Invalid email or token"]
      });
    }

    var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword!);

    if (result.Succeeded) {
      _logger.LogInformation("Password reset successfully for user {Email}", user.Email);
      return Ok(new PasswordResponse {
        Success = true,
        Message = "Password reset successfully"
      });
    }

    _logger.LogInformation("Password reset failed for user {Email}", user.Email);
    return BadRequest(new PasswordResponse {
      Success = false,
      Message = "Password reset failed",
      Errors = result.Errors.Select(e => e.Description)
    });
  }

  private static GetUserResponse UserToGetUserResponse (User user) {
    return new GetUserResponse
    {
        Id = user.Id,
        Email = user.Email!,
        UserName = user.UserName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        ProfilePicture = user.ProfilePicture,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginDate = user.LastLoginDate,
        DisplayName = user.DisplayName
    };
  }
}

