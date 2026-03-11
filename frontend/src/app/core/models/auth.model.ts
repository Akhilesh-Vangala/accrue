export type Role = 'Clerk' | 'Approver' | 'Controller';

export interface Me {
  email: string;
  displayName: string;
  roles: Role[];
}
