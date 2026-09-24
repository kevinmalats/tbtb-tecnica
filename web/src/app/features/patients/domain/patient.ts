export interface Patient {
  id: string;
  fullName: string;
  documentCountryCode: string;
  documentTypeCode: string;
  documentNumber: string;
  phone: string;
  email: string | null;
  cityId: number;
  cityName: string;
  treatmentStartDate: string;
  assignedManagerId: string;
}
