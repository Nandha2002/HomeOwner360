import type {Customer,Dashboard,Loan,PagedLoans,Payment,PaymentHistory,UserSession} from "@/lib/types";
export const API_URL=process.env.NEXT_PUBLIC_API_URL??"http://localhost:5176/api";
function token(){return typeof window==="undefined"?null:localStorage.getItem("homeowner360_token");}
export function saveSession(s:UserSession){localStorage.setItem("homeowner360_token",s.token);localStorage.setItem("homeowner360_session",JSON.stringify(s));}
export function getSession():UserSession|null{if(typeof window==="undefined")return null;const r=localStorage.getItem("homeowner360_session");if(!r)return null;try{return JSON.parse(r)}catch{return null}}
export function clearSession(){localStorage.removeItem("homeowner360_token");localStorage.removeItem("homeowner360_session");}
export async function apiFetch<T>(path:string,options:RequestInit={}):Promise<T>{const h=new Headers(options.headers);h.set("Content-Type","application/json");const t=token();if(t)h.set("Authorization",`Bearer ${t}`);const r=await fetch(`${API_URL}${path}`,{...options,headers:h});const text=await r.text();let b:unknown=null;try{b=text?JSON.parse(text):null}catch{b=text}if(r.status===401){clearSession();if(typeof window!=="undefined")window.location.href="/login"}if(!r.ok){const m=typeof b==="object"&&b&&"message"in b&&typeof b.message==="string"?b.message:`Request failed (${r.status})`;throw new Error(m)}return b as T}
export const login=(username:string,password:string)=>apiFetch<UserSession>("/Auth/login",{method:"POST",body:JSON.stringify({username,password})});
export const register=(username:string,email:string,password:string)=>apiFetch<UserSession>("/Auth/register",{method:"POST",body:JSON.stringify({username,email,password})});
export const getDashboard=()=>apiFetch<Dashboard>("/Dashboard");
export const getCustomers=()=>apiFetch<Customer[]>("/Customers");
export const createCustomer=(d:{name:string;email:string})=>apiFetch<Customer>("/Customers",{method:"POST",body:JSON.stringify(d)});
export const updateCustomer=(id:number,d:{name:string;email:string})=>apiFetch<Customer>(`/Customers/${id}`,{method:"PUT",body:JSON.stringify(d)});
export const deleteCustomer=(id:number)=>apiFetch<void>(`/Customers/${id}`,{method:"DELETE"});
export const getLoans=()=>apiFetch<Loan[]>("/Loans");
export const getLoan=(id:number)=>apiFetch<Loan>(`/Loans/${id}`);
export const getLoanPayments=(id:number)=>apiFetch<Payment[]>(`/Loans/${id}/payments`);
export const searchLoans=(p:Record<string,string|number|boolean|undefined>)=>{const q=new URLSearchParams();Object.entries(p).forEach(([k,v])=>{if(v!==undefined&&v!=="")q.set(k,String(v))});return apiFetch<PagedLoans>(`/Loans/search?${q}`)};
export const getPayments=()=>apiFetch<Payment[]>("/Payments");
export const getPaymentHistory=(id:number,page=1,pageSize=10,status?:string)=>{const q=new URLSearchParams({page:String(page),pageSize:String(pageSize)});if(status)q.set("status",status);return apiFetch<PaymentHistory>(`/Payments/loan/${id}/history?${q}`)};
