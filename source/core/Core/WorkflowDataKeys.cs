namespace Twf.Flow.Core;

/// <summary>
/// Centralized WorkflowData key catalog for cross-node/runtime keys.
/// Keep node-local input/output keys on the node types themselves.
/// </summary>
public static class WorkflowDataKeys
{
    public static class Branch
    {
        public const string RouteStatus = "branch_route_status";
        public const string Status = "branch_status";
        public const string Route = "branch_route";
        public const string SelectedPort = "branch_selected_port";
    }

    public static class ErrorRoute
    {
        public const string ErrorRouteKey = "error_route";
        public const string RouteError = "route_error";
        public const string RouteSuccess = "route_success";
        public const string RoutedErrorMessage = "routed_error_message";
        public const string InputErrorMessage = "error_message";
    }

    public static class Logging
    {
        public const string LoggedKeys = "logged_keys";
        public const string LogLabel = "log_label";
        public const string LogMessage = "log_message";
    }

    public static class TryCatch
    {
        public const string Route = "try_catch_route";
        public const string Success = "try_success";
        public const string Error = "try_error";
        public const string CaughtErrorMessage = "caught_error_message";
        public const string CaughtFailedNode = "caught_failed_node";
        public const string CaughtExceptionType = "caught_exception_type";
    }

    public static class Loop
    {
        public const string LoopItem = "__loop_item__";
        public const string LoopIndex = "__loop_index__";
        public const string LoopTotal = "__loop_total__";
    }

    public static class Pipelines
    {
        public const string SearchResults = "search_results";
    }

    public static class Metadata
    {
        public static class Control
        {
            public const string Route = "route";
            public const string StatusCode = "status_code";
            public const string Threshold = "threshold";
            public const string IterationCount = "iteration_count";
        }

        public static class AI
        {
            public const string Dimensions = "dimensions";
            public const string BatchSize = "batch_size";
            public const string Model = "model";
            public const string PromptTokens = "prompt_tokens";
            public const string CompletionTokens = "completion_tokens";
            public const string ParsedKeys = "parsed_keys";
        }

        public static class IO
        {
            public const string StatusCode = "status_code";
            public const string ResultCount = "result_count";
            public const string Query = "query";
            public const string OutputPath = "output_path";
            public const string BytesWritten = "bytes_written";
            public const string RowCount = "row_count";
            public const string CsvLength = "csv_length";
            public const string FileSize = "file_size";
            public const string FileExtension = "file_extension";
            public const string ColumnCount = "column_count";
        }

        public static class Data
        {
            public const string MappedCount = "mapped_count";
            public const string MissingCount = "missing_count";
            public const string RemoveUnmapped = "remove_unmapped";
            public const string ListKey = "list_key";
            public const string ListCount = "list_count";
            public const string MergedCount = "merged_count";
            public const string RulesChecked = "rules_checked";
            public const string Failures = "failures";
            public const string Removed = "removed";
            public const string OriginalLength = "original_length";
            public const string ResultLength = "result_length";
            public const string ValueKind = "value_kind";
            public const string InputLength = "input_length";
            public const string JsonLength = "json_length";
            public const string ChunkCount = "chunk_count";
            public const string AvgChunkSize = "avg_chunk_size";
            public const string A = "a";
            public const string B = "b";
            public const string Result = "result";
        }
    }
}
