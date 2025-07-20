using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 地址验证器接口
    /// Address validator interface
    /// </summary>
    public interface IAddressValidator
    {
        /// <summary>
        /// 验证地址
        /// Validate address
        /// </summary>
        /// <param name="address">要验证的地址 / Address to validate</param>
        /// <returns>验证结果 / Validation result</returns>
        Task<AddressValidationResult> ValidateAddress(string address);
    }

    /// <summary>
    /// 地址验证结果
    /// Address validation result
    /// </summary>
    public class AddressValidationResult
    {
        /// <summary>
        /// 地址是否有效
        /// Whether the address is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 地址类型
        /// Address type
        /// </summary>
        public string AddressType { get; set; }

        /// <summary>
        /// 网络类型
        /// Network type
        /// </summary>
        public string Network { get; set; }

        /// <summary>
        /// 错误信息
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
