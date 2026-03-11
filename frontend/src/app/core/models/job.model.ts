export interface RecurringJob {
  id: string;
  description: string;
  cron: string;
  lastExecution: string | null;
  lastJobState: string | null;
  nextExecution: string | null;
}

export interface TriggeredJob {
  recurringJobId: string;
  backgroundJobId: string;
}
