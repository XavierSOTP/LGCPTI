using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using System.Collections.Generic;

namespace LGC.PTI.Common.Application.Dtos;

/// <summary>
/// Represents the response returned when retrieving employees.
/// </summary>
public class EmployeesResponse
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the collection of errors returned by the operation.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0016:Prefer returning collection abstraction instead of implementation", Justification = "<Pending>")]
    public List<string> Errors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result containing the employee data and pagination metadata.
    /// </summary>
    public Result Result { get; set; }

    #endregion Public Properties
}

/// <summary>
/// Represents pagination metadata associated with an employee result set.
/// </summary>
public class MetaData
{
    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether another page of results is available.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages available.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items contained in a page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items available across all pages.
    /// </summary>
    public int TotalItemCount { get; set; }

    #endregion Public Properties
}

/// <summary>
/// Represents the employee data returned by the operation together with its pagination metadata.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0048:File name must match type name", Justification = "<Pending>")]
public class Result
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the collection of employees returned by the operation.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0016:Prefer returning collection abstraction instead of implementation", Justification = "<Pending>")]
    public List<EmployeeInDto> Items { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata associated with the employee collection.
    /// </summary>
    public MetaData MetaData { get; set; }

    #endregion Public Properties
}