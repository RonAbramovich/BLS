using Microsoft.AspNetCore.Mvc;
using BLS.WebAPI.Models;
using BLS.WebAPI.Services;

namespace BLS.WebAPI.Controllers
{
    /// <summary>
    /// API Controller for BLS aggregated signatures
    /// </summary>
    [ApiController]
    [Route("api/aggregated")]
    public class AggregatedSignatureController : ControllerBase
    {
        private readonly AggregatedSignatureService _aggregatedService;

        public AggregatedSignatureController()
        {
            _aggregatedService = new AggregatedSignatureService();
        }

        /// <summary>
        /// Get private key constraints and validation examples
        /// </summary>
        /// <param name="request">Request with curve parameters</param>
        /// <returns>Valid and invalid private key examples with explanations</returns>
        [HttpPost("private-key-constraints")]
        [ProducesResponseType(typeof(PrivateKeyConstraintsResponse), 200)]
        [ProducesResponseType(400)]
        public ActionResult<PrivateKeyConstraintsResponse> GetPrivateKeyConstraints([FromBody] PrivateKeyConstraintsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PrivateKeyConstraintsResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid input parameters."
                });
            }

            try
            {
                var response = _aggregatedService.GetPrivateKeyConstraints(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new PrivateKeyConstraintsResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Execute aggregated signature
        /// </summary>
        /// <param name="request">Aggregated signature request</param>
        /// <returns>Aggregated signature response with steps</returns>
        [HttpPost("sign")]
        [ProducesResponseType(typeof(AggregatedSignatureResponse), 200)]
        [ProducesResponseType(400)]
        public ActionResult<AggregatedSignatureResponse> ExecuteAggregatedSignature([FromBody] AggregatedSignatureRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AggregatedSignatureResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid input parameters."
                });
            }

            try
            {
                var response = _aggregatedService.ExecuteAggregatedSignature(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new AggregatedSignatureResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                });
            }
        }
    }
}
