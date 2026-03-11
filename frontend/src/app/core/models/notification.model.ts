export interface NotificationItem {
  id: number;
  title: string;
  body: string;
  link: string | null;
  createdAt: string;
  isRead: boolean;
}

export interface NotificationFeed {
  unreadCount: number;
  items: NotificationItem[];
}
