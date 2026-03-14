using Microsoft.AspNetCore.Mvc;
using BLS.WebAPI.Models;
using BLS.WebAPI.Services;

namespace BLS.WebAPI.Controllers
{
    /// <summary>
    /// API Controller for BLS signature generation and verification
    /// </summary>
    [ApiController]
    [Route("api/bls")]
    public class BLSSignatureController : ControllerBase
    {
        private readonly BLSSignatureService _signatureService;

        public BLSSignatureController()
        {
            _signatureService = new BLSSignatureService();
        }

        /// <summary>
        /// Generate BLS signature and verify it step-by-step
        /// </summary>
        /// <param name="request">Signature request with curve parameters, private key, and message</param>
        /// <returns>Step-by-step calculation results with signature and verification</returns>
        /// <response code="200">Successfully generated and verified signature</response>
        /// <response code="400">Invalid input parameters</response>
        [HttpPost("sign")]
        [ProducesResponseType(typeof(BLSSignatureResponse), 200)]
        [ProducesResponseType(400)]
        public ActionResult<BLSSignatureResponse> GenerateSignature([FromBody] BLSSignatureRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new BLSSignatureResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid input parameters. Please check all required fields."
                });
            }

            try
            {
                var response = _signatureService.ExecuteSignature(request);
                
                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new BLSSignatureResponse
                {
                    Success = false,
                    ErrorMessage = $"Error processing request: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "BLS Signature API",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
