package workflowdiagramming

import "strings"

// RenderMermaid builds a linear flowchart TD for v1.
func RenderMermaid(steps []DiagramStep) string {
	var b strings.Builder
	b.WriteString("flowchart TD\n")
	b.WriteString("    Start([Start])\n")
	ids := []string{"Start"}
	for _, step := range steps {
		id := sanitizeNode(step.ID)
		ids = append(ids, id)
		label := strings.ReplaceAll(step.Label, `"`, "'")
		if step.Kind == StepWait {
			b.WriteString("    " + id + `["👤 ` + label + `"]` + "\n")
		} else {
			b.WriteString("    " + id + `["` + label + `"]` + "\n")
		}
	}
	b.WriteString("    End([\"Complete\"])\n")
	ids = append(ids, "End")
	for i := 0; i < len(ids)-1; i++ {
		b.WriteString("    " + ids[i] + " --> " + ids[i+1] + "\n")
	}
	return b.String()
}

func sanitizeNode(name string) string {
	var b strings.Builder
	for _, r := range name {
		if (r >= 'a' && r <= 'z') || (r >= 'A' && r <= 'Z') || (r >= '0' && r <= '9') {
			b.WriteRune(r)
		}
	}
	s := b.String()
	if s == "" {
		return "Node"
	}
	return s
}
