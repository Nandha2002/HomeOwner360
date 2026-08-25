export interface UserSession { token:string; username:string; role:string; expiresAt:string; }
export interface Customer { customerId:number; name:string; email:string; }
export interface Loan { loanId:number; customerId:number; loanNumber:string; originalAmount:number; currentBalance:number; interestRate:number; }
export interface Payment { paymentId:number; loanId:number; amount:number; status:string; paymentDate:string; }
export interface Dashboard { totalCustomers:number; totalLoans:number; totalLoanAmount:number; totalOutstandingBalance:number; totalPayments:number; totalPaymentsCount:number; }
export interface PaymentHistory { payments:Payment[]; page:number; pageSize:number; totalRecords:number; totalPages:number; }
export interface PagedLoans { items:Loan[]; page:number; pageSize:number; totalRecords:number; totalPages:number; }
