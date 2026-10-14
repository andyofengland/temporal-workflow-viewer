package workflowdiagramming

// WorkflowDiagramsMetadata is the shared zip contract (packageName primary).
type WorkflowDiagramsMetadata struct {
	PackageName    string          `json:"packageName"`
	PackageVersion string          `json:"packageVersion,omitempty"`
	PackagePath    string          `json:"packagePath,omitempty"`
	Language       string          `json:"language"`
	TargetFramework string         `json:"targetFramework,omitempty"`
	BuildDateUtc   string          `json:"buildDateUtc"`
	Generator      string          `json:"generator"`
	Workflows      []WorkflowEntry `json:"workflows"`
}

// WorkflowEntry indexes one Mermaid diagram file.
type WorkflowEntry struct {
	Name        string `json:"name"`
	DisplayName string `json:"displayName,omitempty"`
	DiagramFile string `json:"diagramFile"`
}

// StepKind classifies a diagram node.
type StepKind string

const (
	StepActivity StepKind = "activity"
	StepWait     StepKind = "wait"
	StepChild    StepKind = "child"
	StepParallel StepKind = "parallel"
)

// DiagramStep is one node in a linear v1 diagram.
type DiagramStep struct {
	ID    string
	Label string
	Kind  StepKind
}

// ExtractedWorkflow is a discovered Temporal workflow function.
type ExtractedWorkflow struct {
	Name        string
	DisplayName string
	Steps       []DiagramStep
	SourceFile  string
}
