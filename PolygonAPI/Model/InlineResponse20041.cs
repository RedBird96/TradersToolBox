/* 
 * Polygon API
 *
 * The future of fintech.
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PolygonIO.Model
{
    /// <summary>
    /// InlineResponse20041
    /// </summary>
    [DataContract]
    public partial class InlineResponse20041 :  IEquatable<InlineResponse20041>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineResponse20041" /> class.
        /// </summary>
        /// <param name="tickers">tickers.</param>
        public InlineResponse20041(List<CryptoSnapshotTickerTicker> tickers = default(List<CryptoSnapshotTickerTicker>))
        {
            this.Tickers = tickers;
        }
        
        /// <summary>
        /// Gets or Sets Tickers
        /// </summary>
        [DataMember(Name="tickers", EmitDefaultValue=false)]
        public List<CryptoSnapshotTickerTicker> Tickers { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InlineResponse20041 {\n");
            sb.Append("  Tickers: ").Append(Tickers).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as InlineResponse20041);
        }

        /// <summary>
        /// Returns true if InlineResponse20041 instances are equal
        /// </summary>
        /// <param name="input">Instance of InlineResponse20041 to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(InlineResponse20041 input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Tickers == input.Tickers ||
                    this.Tickers != null &&
                    input.Tickers != null &&
                    this.Tickers.SequenceEqual(input.Tickers)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Tickers != null)
                    hashCode = hashCode * 59 + this.Tickers.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}