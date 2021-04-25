using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	internal class VidPid
	{
		public string Vid { get; }
		public string Pid { get; }

		public bool IsValid { get; }

		private static readonly Regex _pattern = new Regex(@"USB(\\|#)VID_(?<vid>[0-9a-fA-F]{4})&PID_(?<pid>[0-9a-fA-F]{4})(&|#)");

		public VidPid(string source)
		{
			if (string.IsNullOrWhiteSpace(source))
				return;

			var match = _pattern.Match(source);
			if (match.Success)
			{
				Vid = match.Groups["vid"].Value;
				Pid = match.Groups["pid"].Value;
				IsValid = true;
			}
		}

		public override bool Equals(object obj) =>
			(obj is VidPid other)
			&& string.Equals(this.Vid, other.Vid, StringComparison.Ordinal)
			&& string.Equals(this.Pid, other.Pid, StringComparison.Ordinal);

		public override int GetHashCode() => IsValid ? (Vid.GetHashCode() ^ Pid.GetHashCode()) : 0;

		public override string ToString() => $"{{VID={Vid}, PID={Pid}}}";
	}
}