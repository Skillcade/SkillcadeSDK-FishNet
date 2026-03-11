namespace SkillcadeSDK.Replays
{
    public readonly struct ReplayFileResult
    {
        public bool IsSuccess { get; }
        public bool IsCancelled { get; }
        public string FileName { get; }
        public string FilePath { get; } // null для WebGL
        public byte[] Data { get; }
        public string Error { get; }

        private ReplayFileResult(bool success, bool cancelled, string fileName, string filePath, byte[] data, string error)
        {
            IsSuccess = success;
            IsCancelled = cancelled;
            FileName = fileName;
            FilePath = filePath;
            Data = data;
            Error = error;
        }

        public static ReplayFileResult Success(string fileName, string filePath, byte[] data)
            => new(true, false, fileName, filePath, data, null);

        public static ReplayFileResult Cancelled()
            => new(false, true, null, null, null, null);

        public static ReplayFileResult Failed(string error)
            => new(false, false, null, null, null, error);

        public override string ToString()
        {
            if (IsCancelled)
                return "Cancelled file load";

            if (!IsSuccess)
                return $"Failed file load: {Error}";

            return $"File loaded, name: {FileName}, path: {FilePath}, error: {Error}, content length: {Data?.Length ?? 0}";
        }
    }
}