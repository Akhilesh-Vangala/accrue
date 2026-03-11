export interface ReportDefinition {
  name: string;
  title: string;
  description: string;
  parameters: string[];
}

export interface ReportCapabilities {
  engine: string;
  supportsPdf: boolean;
  interactiveViewer: boolean;
  reports: ReportDefinition[];
}

export interface ReportArchive {
  id: number;
  reportName: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  generatedBy: string;
  generatedAt: string;
}
