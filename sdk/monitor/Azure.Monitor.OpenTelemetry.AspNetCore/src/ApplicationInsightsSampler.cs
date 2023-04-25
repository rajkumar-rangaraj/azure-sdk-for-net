// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenTelemetry.Trace;
using System.Collections.Generic;
using System;

namespace Azure.Monitor.OpenTelemetry.AspNetCore;

/// <summary>
/// Sample configurable for OpenTelemetry exporters for compatibility
/// with Application Insight SDKs.
/// </summary>
public class ApplicationInsightsSampler : Sampler
{
    private static readonly SamplingResult RecordOnlySamplingResult = new(SamplingDecision.RecordOnly);
    private readonly SamplingResult recordAndSampleSamplingResult;
    private readonly float samplingRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsSampler"/> class.
    /// </summary>
    /// <param name="samplingRatio">
    /// Ratio of telemetry that should be sampled.
    /// For example; Specifying 0.4F means 40% of traces are sampled and 60% are dropped.
    /// </param>
    public ApplicationInsightsSampler(float samplingRatio)
    {
        // Ensure passed ratio is between 0 and 1, inclusive
        this.samplingRatio = samplingRatio;
        this.Description = "ApplicationInsightsSampler{" + samplingRatio + "}";
        var sampleRate = (float)Math.Round(samplingRatio * 100);
        this.recordAndSampleSamplingResult = new SamplingResult(
            SamplingDecision.RecordAndSample,
            new Dictionary<string, object>
                {
                    { "sampleRate", sampleRate },
                });
    }

    /// <summary>
    /// test.
    /// </summary>
    /// <param name="samplerOptions">test.</param>
    public ApplicationInsightsSampler(ApplicationInsightsSamplerOptions samplerOptions) : this(samplerOptions.samplingRatio)
    {
    }

    /// <summary>
    /// Computational method using the DJB2 Hash algorithm to decide whether to sample
    /// a given telemetry item, based on its Trace Id.
    /// </summary>
    /// <param name="samplingParameters">Parameters of telemetry item used to make sampling decision.</param>
    /// <returns>Returns whether or not we should sample telemetry in the form of a <see cref="SamplingResult"/> class.</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        if (this.samplingRatio == 0)
        {
            return RecordOnlySamplingResult;
        }

        if (this.samplingRatio == 1)
        {
            return this.recordAndSampleSamplingResult;
        }

        double sampleScore = DJB2SampleScore(samplingParameters.TraceId.ToHexString().ToUpperInvariant());

        if (sampleScore < this.samplingRatio)
        {
            return this.recordAndSampleSamplingResult;
        }
        else
        {
            return RecordOnlySamplingResult;
        }
    }

    private static double DJB2SampleScore(string traceIdHex)
    {
        // Calculate DJB2 hash code from hex-converted TraceId
        int hash = 5381;

        for (int i = 0; i < traceIdHex.Length; i++)
        {
            unchecked
            {
                hash = (hash << 5) + hash + (int)traceIdHex[i];
            }
        }

        // Take the absolute value of the hash
        if (hash == int.MinValue)
        {
            hash = int.MaxValue;
        }
        else
        {
            hash = Math.Abs(hash);
        }

        // Divide by MaxValue for value between 0 and 1 for sampling score
        double samplingScore = (double)hash / int.MaxValue;
        return samplingScore;
    }
}
