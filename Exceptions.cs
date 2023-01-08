using System;
using System.Runtime.Serialization;

namespace RankBot;

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

public class UbisoftApiException: Exception
{
    public UbisoftApiException()
    {
    }

    public UbisoftApiException(string message)
        : base(message)
    {
    }

    public UbisoftApiException(string message, Exception inner)
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

public class GuildStructureException : Exception
{
    public GuildStructureException()
    {
    }

    public GuildStructureException(string message)
        : base(message)
    {
    }

    public GuildStructureException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class GuildListException : Exception
{
    public GuildListException()
    {
    }

    public GuildListException(string message)
        : base(message)
    {
    }

    public GuildListException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
public class BanParsingException : Exception
{
    public BanParsingException()
    {
    }

    public BanParsingException(string message)
        : base(message)
    {
    }

    public BanParsingException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

[Serializable]
public class BackupException : Exception
{
    public BackupException()
    {
    }

    public BackupException(string message) : base(message)
    {
    }

    public BackupException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BackupException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}