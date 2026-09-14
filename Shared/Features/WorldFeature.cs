using System;
using System.Collections.Generic;

public abstract record WorldFeature
{
	private static readonly HashSet<Type> NONE = new();

	public virtual HashSet<Type> Excludes => NONE;

	public virtual int MinimumChunkCount => 0;
}
