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
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = PolygonIO.Client.SwaggerDateConverter;

namespace PolygonIO.Model
{
    /// <summary>
    /// RatingSection
    /// </summary>
    [DataContract]
        public partial class RatingSection :  IEquatable<RatingSection>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RatingSection" /> class.
        /// </summary>
        /// <param name="current">Analyst Rating at current month (required).</param>
        /// <param name="month1">Analyst Ratings at 1 month in the future (required).</param>
        /// <param name="month2">Analyst Ratings at 2 month in the future (required).</param>
        /// <param name="month3">Analyst Ratings at 3 month in the future (required).</param>
        /// <param name="month4">Analyst Ratings at 4 month in the future.</param>
        /// <param name="month5">Analyst Ratings at 5 month in the future.</param>
        public RatingSection(decimal? current = default(decimal?), decimal? month1 = default(decimal?), decimal? month2 = default(decimal?), decimal? month3 = default(decimal?), decimal? month4 = default(decimal?), decimal? month5 = default(decimal?))
        {
            // to ensure "current" is required (not null)
            if (current == null)
            {
                throw new InvalidDataException("current is a required property for RatingSection and cannot be null");
            }
            else
            {
                this.Current = current;
            }
            // to ensure "month1" is required (not null)
            if (month1 == null)
            {
                throw new InvalidDataException("month1 is a required property for RatingSection and cannot be null");
            }
            else
            {
                this.Month1 = month1;
            }
            // to ensure "month2" is required (not null)
            if (month2 == null)
            {
                throw new InvalidDataException("month2 is a required property for RatingSection and cannot be null");
            }
            else
            {
                this.Month2 = month2;
            }
            // to ensure "month3" is required (not null)
            if (month3 == null)
            {
                throw new InvalidDataException("month3 is a required property for RatingSection and cannot be null");
            }
            else
            {
                this.Month3 = month3;
            }
            this.Month4 = month4;
            this.Month5 = month5;
        }
        
        /// <summary>
        /// Analyst Rating at current month
        /// </summary>
        /// <value>Analyst Rating at current month</value>
        [DataMember(Name="current", EmitDefaultValue=false)]
        public decimal? Current { get; set; }

        /// <summary>
        /// Analyst Ratings at 1 month in the future
        /// </summary>
        /// <value>Analyst Ratings at 1 month in the future</value>
        [DataMember(Name="month1", EmitDefaultValue=false)]
        public decimal? Month1 { get; set; }

        /// <summary>
        /// Analyst Ratings at 2 month in the future
        /// </summary>
        /// <value>Analyst Ratings at 2 month in the future</value>
        [DataMember(Name="month2", EmitDefaultValue=false)]
        public decimal? Month2 { get; set; }

        /// <summary>
        /// Analyst Ratings at 3 month in the future
        /// </summary>
        /// <value>Analyst Ratings at 3 month in the future</value>
        [DataMember(Name="month3", EmitDefaultValue=false)]
        public decimal? Month3 { get; set; }

        /// <summary>
        /// Analyst Ratings at 4 month in the future
        /// </summary>
        /// <value>Analyst Ratings at 4 month in the future</value>
        [DataMember(Name="month4", EmitDefaultValue=false)]
        public decimal? Month4 { get; set; }

        /// <summary>
        /// Analyst Ratings at 5 month in the future
        /// </summary>
        /// <value>Analyst Ratings at 5 month in the future</value>
        [DataMember(Name="month5", EmitDefaultValue=false)]
        public decimal? Month5 { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class RatingSection {\n");
            sb.Append("  Current: ").Append(Current).Append("\n");
            sb.Append("  Month1: ").Append(Month1).Append("\n");
            sb.Append("  Month2: ").Append(Month2).Append("\n");
            sb.Append("  Month3: ").Append(Month3).Append("\n");
            sb.Append("  Month4: ").Append(Month4).Append("\n");
            sb.Append("  Month5: ").Append(Month5).Append("\n");
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
            return this.Equals(input as RatingSection);
        }

        /// <summary>
        /// Returns true if RatingSection instances are equal
        /// </summary>
        /// <param name="input">Instance of RatingSection to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(RatingSection input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Current == input.Current ||
                    (this.Current != null &&
                    this.Current.Equals(input.Current))
                ) && 
                (
                    this.Month1 == input.Month1 ||
                    (this.Month1 != null &&
                    this.Month1.Equals(input.Month1))
                ) && 
                (
                    this.Month2 == input.Month2 ||
                    (this.Month2 != null &&
                    this.Month2.Equals(input.Month2))
                ) && 
                (
                    this.Month3 == input.Month3 ||
                    (this.Month3 != null &&
                    this.Month3.Equals(input.Month3))
                ) && 
                (
                    this.Month4 == input.Month4 ||
                    (this.Month4 != null &&
                    this.Month4.Equals(input.Month4))
                ) && 
                (
                    this.Month5 == input.Month5 ||
                    (this.Month5 != null &&
                    this.Month5.Equals(input.Month5))
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
                if (this.Current != null)
                    hashCode = hashCode * 59 + this.Current.GetHashCode();
                if (this.Month1 != null)
                    hashCode = hashCode * 59 + this.Month1.GetHashCode();
                if (this.Month2 != null)
                    hashCode = hashCode * 59 + this.Month2.GetHashCode();
                if (this.Month3 != null)
                    hashCode = hashCode * 59 + this.Month3.GetHashCode();
                if (this.Month4 != null)
                    hashCode = hashCode * 59 + this.Month4.GetHashCode();
                if (this.Month5 != null)
                    hashCode = hashCode * 59 + this.Month5.GetHashCode();
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
