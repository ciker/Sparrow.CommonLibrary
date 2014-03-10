﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Retrying
{
    /// <summary>
    /// 无重试规则
    /// </summary>
    public class NonRetryInterval : RetryStrategy
    {
        public NonRetryInterval()
            : base(null, false, 0)
        {
        }

        protected override bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            return false;
        }
    }
}
