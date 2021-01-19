﻿using System;

namespace R6RankBot
{
    public class RankParsingException : Exception
    {
        public RankParsingException()
        {
        }

        public RankParsingException(string message)
            : base(message)
        {
        }

        public RankParsingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class DuplicateException : Exception
    {
        public DuplicateException()
        {
        }

        public DuplicateException(string message)
            : base(message)
        {
        }

        public DuplicateException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class DoNotTrackException : Exception
    {
        public DoNotTrackException()
        {
        }

        public DoNotTrackException(string message)
            : base(message)
        {
        }

        public DoNotTrackException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}