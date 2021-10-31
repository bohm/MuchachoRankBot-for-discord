﻿using System;

namespace RankBot
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

    public class PrimaryGuildException : Exception
    {
        public PrimaryGuildException()
        {
        }

        public PrimaryGuildException(string message)
            : base(message)
        {
        }

        public PrimaryGuildException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}