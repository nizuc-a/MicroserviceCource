namespace MicroserviceCourse.Model.DTO.Pagination;

public class PaginatedResult<T>
{
    public required T[] Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}