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
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  // TODO: Checker pourquoi ActionResult et pas IActionResult
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

    // Pq request.Password!
    var result = await _userManager.CreateAsync(user, request.Password!);

    if (result.Succeeded) {
      user.LastLoginDate = DateTime.UtcNow;
      await _userManager.UpdateAsync(user);

      await _signInManager.SignInAsync(user, isPersistent: false);

      return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new AuthResponse {
        IsAuthenticated = true,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        LastLoginDate = user.LastLoginDate,
        Message = "User registered successfully" 
      });
    }

    return BadRequest(new AuthResponse
    {
      IsAuthenticated = false,
      Message = "Registration failed",
      Errors = result.Errors.Select(e => e.Description)
    });
  }

  /// <summary>
  /// Login a user
  /// </summary>
  /// <param name="request">The user credentials.</param>
  /// <returns>The logged User</returns>
  [HttpPost("login")]
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Login([FromBody] LoginRequest request)
  {
    var user = await _userManager.FindByEmailAsync(request.Email!);

    if (user == null) {
      _logger.LogInformation("User not found while login {email}.", request.Email);
      return BadRequest(new AuthResponse 
      { 
          IsAuthenticated = false,
          // TODO: log unfound email
          Message = "Login failed."
      });
    }

    if (!user.IsActive) {
      _logger.LogInformation("User {email} deactivated.", request.Email);
      return BadRequest(new AuthResponse 
      { 
          IsAuthenticated = false,
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

      return Ok(new AuthResponse{
        IsAuthenticated = true,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        LastLoginDate = user.LastLoginDate,
        Message = "Login successful" 
      });
    }

    if (result.IsLockedOut)
    {
      _logger.LogInformation("User {email} account locked.", request.Email);
      return BadRequest(new AuthResponse 
      { 
          IsAuthenticated = false,
          Message = "Account locked out. Please try again later." 
      });
    }

    _logger.LogInformation("Invalid email or password while login {email}.", request.Email);
    return BadRequest(new AuthResponse 
    { 
      IsAuthenticated = false,
      Message = "Invalid email or password" 
    });
  }

  /// <summary>
  /// Get all users with optional filtering and pagination.
  /// </summary>
  /// <param name="request">Filtering and pagination parameters.</param>
  /// <returns>An array of users.</returns>
  [HttpGet]
  [Authorize]
  [ProducesResponseType(typeof(IEnumerable<GetUserResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersRequest? request) 
  {
    //Todo : pq ne pas utiliser dbContext
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

    return Ok(users.Select(UserToGetUserResponse));
  }

  /// <summary>
  /// Get a user by ID.
  /// </summary>
  /// <param name="id">The ID of the user.</param>
  /// <returns>The single user record.</returns>
  [HttpGet("{id}")]
  [Authorize]
  [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetUserById([FromRoute] string id) {
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
  [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetCurrentUser()
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
  [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
  {
    // TODO: log update
    
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
  /// Update a user by ID.
  /// </summary>
  /// <param name="id">The ID of the user to update.</param>
  /// <param name="request">User update data.</param>
  /// <returns>Updated user information.</returns>
  [HttpPut("{id}")]
  [Authorize] // Add role-based authorization later if needed
  [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
  {
    _logger.LogInformation("Updating user with ID: {UserId}", id);

    // Use DbContext with tracking for updates - same pattern as Employees
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
  /// <param name="id">The ID of the user to deactivate.</param>
  /// <returns>No content on success.</returns>
  [HttpDelete("{id}")]
  [Authorize] // Add role-based authorization later if needed
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
    return NoContent();
  }

  /// <summary>
  /// Logout current user
  /// </summary>
  /// <returns>Authentication response</returns>
  [HttpPost("logout")]
  [Authorize]
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Logout()
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
  [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public IActionResult GetAuthStatus()
  {
    return Ok(new AuthResponse
    { 
        IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
        UserName = User.Identity?.Name,
        Message = "Authentication status retrieved successfully"
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
        DisplayName = user.DisplayName
    };
  }
}

