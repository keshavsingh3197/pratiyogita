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

export interface Location {
  id: string;
  villageOrTown?: string | null;
  city: string;
  district?: string | null;
  state: string;
  country: string;
}

export interface CompetitionCategory {
  id: string;
  name: string;
  type: CompetitionType;
}

export interface PaymentSettings {
  configured: boolean;
  upiVpa?: string | null;
  payeeName: string;
  hasUploadedQr: boolean;
}

export interface ContributionItem {
  id: string;
  name: string;
  description?: string | null;
  amount: number;
  isActive: boolean;
}

export interface StudentProfile {
  id: string;
  schoolId: string;
  firstName: string;
  lastName?: string | null;
  dateOfBirth?: string | null;
  classGrade?: string | null;
  academicYear?: string | null;
  email?: string | null;
  phone?: string | null;
  guardianName?: string | null;
}
