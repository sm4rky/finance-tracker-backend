using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finance_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AnalyticsController(
    INetWorthService netWorthService,
    ICashflowService cashflowService,
    ICategoryExpenseDistributionService categoryExpenseDistributionService,
    IStackedExpensesByCategoryService stackedExpensesByCategoryService,
    IGroupedExpensesByAccountService groupedExpensesByAccountService)
    : ControllerBase
{
    [HttpGet("net-worth")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(NetWorthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NetWorthResponse>> GetNetWorth(CancellationToken cancellationToken)
    {
        var dto = await netWorthService.GetNetWorthAsync(User, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("net-worth/history")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MonthlyNetWorthHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MonthlyNetWorthHistoryResponse>> GetMonthlyNetWorthHistory(
        [FromQuery] MonthlyNetWorthHistoryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await netWorthService
            .GetMonthlyNetWorthHistoryAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("cashflow")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CashflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CashflowResponse>> GetCashflow(
        [FromQuery] TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await cashflowService.GetAsync(User, request, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("expense/category-distribution")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CategoryExpenseDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CategoryExpenseDistributionResponse>> GetCategoryExpenseDistribution(
        [FromQuery] TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await categoryExpenseDistributionService
            .GetAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("expense/stacked-by-category")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StackedExpensesByCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StackedExpensesByCategoryResponse>> GetStackedExpensesByCategory(
        [FromQuery] StackedExpensesByCategoryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await stackedExpensesByCategoryService
            .GetAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("expense/grouped-by-account")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GroupedExpensesByAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GroupedExpensesByAccountResponse>> GetGroupedExpensesByAccount(
        [FromQuery] GroupedExpensesByAccountQueryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await groupedExpensesByAccountService
            .GetAsync(User, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
