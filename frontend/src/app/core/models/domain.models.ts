export type CompetitionType = 'Academic' | 'Sports';
export type CompetitionStatus = 'Draft' | 'RegistrationOpen' | 'Ongoing' | 'Completed' | 'Cancelled';

export interface Competition {
  id: string;
  name: string;
  type: CompetitionType;
  category: string;
  level: string;
  description?: string | null;
  venue?: string | null;
  registrationOpensAt?: string | null;
  registrationClosesAt?: string | null;
  startsAt: string;
  endsAt?: string | null;
  status: CompetitionStatus;
}

export interface School {
  id: string;
  name: string;
  code?: string | null;
  locationId?: string | null;
  contactEmail: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Suspended';
}

export interface NewsPost {
  id: string;
  title: string;
  slug: string;
  summary?: string | null;
  body: string;
  coverImageUrl?: string | null;
  tags: string[];
  publishedAt?: string | null;
}

export interface TopperEntry {
  studentProfileId: string;
  studentName: string;
  schoolId: string;
  schoolName: string;
  city?: string | null;
  state?: string | null;
  competitionId: string;
  competitionName: string;
  category: string;
  score?: number | null;
  rank?: number | null;
}

export interface TopContributor {
  name: string;
  total: number;
  contributionCount: number;
}
