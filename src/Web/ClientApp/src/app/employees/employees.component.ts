import {
  Component, OnInit, ViewChild, ElementRef, signal, computed
} from '@angular/core';
import {
  EmployeesClient,
  EmployeeDto,
  EmployeeSearchRequest,
  CreateEmployeeCommand,
  UpdateEmployeeCommand,
  SourceTypeLookupDto,
  ActivePassiveLookupDto
} from '../web-api-client';

interface EmployeeForm {
  registrationNumber: string;
  identityNumber: string;
  firstname: string;
  lastname: string;
  personalMobileNumber: string;
  sourceTypeStr: string;
  activePassiveCode: string;
  isTerminated: boolean;
  companyName: string;
  businessUnitId: number | null;
  description: string;
}

@Component({
  standalone: false,
  selector: 'app-employees',
  templateUrl: './employees.component.html',
  styleUrls: ['./employees.component.scss']
})
export class EmployeesComponent implements OnInit {
  @ViewChild('employeeDialog') employeeDialogRef: ElementRef<HTMLDialogElement>;

  // ── Lookup data ─────────────────────────────────────────────────────────────
  sourceTypes = signal<SourceTypeLookupDto[]>([]);
  activePassiveCodes = signal<ActivePassiveLookupDto[]>([]);

  /** Admin users see SAP / OzonTekstil in their source type list */
  isAdmin = computed(() =>
    this.sourceTypes().some(st => st.value === 'SAP' || st.value === 'OzonTekstil')
  );

  // ── Search state ─────────────────────────────────────────────────────────────
  filter: EmployeeSearchRequest = new EmployeeSearchRequest();
  selectedSourceTypes: string[] = [];
  employees = signal<EmployeeDto[] | null>(null);
  loadingSearch = signal(false);
  searchError = signal('');
  hasSearched = signal(false);

  // ── Create / Edit dialog state ────────────────────────────────────────────────
  isEditing = signal(false);
  savingEmployee = signal(false);
  dialogTitle = computed(() => this.isEditing() ? 'Edit Employee' : 'New Employee');
  formErrors = signal<Record<string, string[]>>({});
  form: EmployeeForm = this.emptyForm();
  loadingNextRegNo = signal(false);

  constructor(private employeesClient: EmployeesClient) {}

  ngOnInit(): void {
    this.employeesClient.getSourceTypeLookups().subscribe({
      next: types => this.sourceTypes.set(types),
      error: err => console.error('Failed to load source types', err)
    });
    this.employeesClient.getActivePassiveCodeLookups().subscribe({
      next: codes => this.activePassiveCodes.set(codes),
      error: err => console.error('Failed to load active/passive codes', err)
    });
  }

  // ── Source type multi-select helpers ─────────────────────────────────────────
  toggleSourceType(value: string): void {
    const idx = this.selectedSourceTypes.indexOf(value);
    if (idx >= 0) {
      this.selectedSourceTypes = this.selectedSourceTypes.filter(v => v !== value);
    } else {
      this.selectedSourceTypes = [...this.selectedSourceTypes, value];
    }
  }

  isSourceTypeSelected(value: string): boolean {
    return this.selectedSourceTypes.includes(value);
  }

  // ── Search ────────────────────────────────────────────────────────────────────
  search(): void {
    this.searchError.set('');
    this.loadingSearch.set(true);
    this.hasSearched.set(true);

    const req = new EmployeeSearchRequest({
      registrationNumber: this.filter.registrationNumber || undefined,
      identityNumber: this.filter.identityNumber || undefined,
      firstName: this.filter.firstName || undefined,
      lastName: this.filter.lastName || undefined,
      activePassiveCode: this.filter.activePassiveCode || undefined,
      isTerminated: this.filter.isTerminated ?? undefined,
      sourceTypeList: this.selectedSourceTypes.length ? this.selectedSourceTypes : undefined
    });

    this.employeesClient.searchEmployees(req).subscribe({
      next: results => {
        this.employees.set(results);
        this.loadingSearch.set(false);
      },
      error: err => {
        this.loadingSearch.set(false);
        try {
          const parsed = JSON.parse(err.response);
          const errors: string[] = [];
          if (parsed?.errors) {
            for (const field of Object.values(parsed.errors)) {
              errors.push(...(field as string[]));
            }
          }
          this.searchError.set(errors.join(' ') || 'Search failed.');
        } catch {
          this.searchError.set('Search failed.');
        }
      }
    });
  }

  clearSearch(): void {
    this.filter = new EmployeeSearchRequest();
    this.selectedSourceTypes = [];
    this.employees.set(null);
    this.hasSearched.set(false);
    this.searchError.set('');
  }

  // ── Create dialog ─────────────────────────────────────────────────────────────
  showCreateDialog(): void {
    this.isEditing.set(false);
    this.form = this.emptyForm();
    this.formErrors.set({});
    this.employeeDialogRef.nativeElement.showModal();

    // Pre-fill registration number preview
    const defaultSourceType = this.isAdmin() ? 'Ecrou' : 'Other';
    this.form.sourceTypeStr = defaultSourceType;
    this.loadNextRegistrationNumber(defaultSourceType);
  }

  onSourceTypeChange(): void {
    if (!this.isEditing() && this.form.sourceTypeStr) {
      this.loadNextRegistrationNumber(this.form.sourceTypeStr);
    }
  }

  private loadNextRegistrationNumber(sourceType: string): void {
    this.loadingNextRegNo.set(true);
    this.employeesClient.getNextRegistrationNumber(sourceType).subscribe({
      next: regNo => {
        this.form.registrationNumber = regNo;
        this.loadingNextRegNo.set(false);
      },
      error: () => this.loadingNextRegNo.set(false)
    });
  }

  // ── Edit dialog ───────────────────────────────────────────────────────────────
  showEditDialog(employee: EmployeeDto): void {
    this.isEditing.set(true);
    this.formErrors.set({});
    this.form = {
      registrationNumber: employee.registrationNumber ?? '',
      identityNumber: employee.identityNumber ?? '',
      firstname: employee.firstname ?? '',
      lastname: employee.lastname ?? '',
      personalMobileNumber: employee.personalMobileNumber ?? '',
      sourceTypeStr: employee.sourceTypeStr ?? '',
      activePassiveCode: employee.activePassiveCode ?? '1',
      isTerminated: employee.isTerminated ?? false,
      companyName: employee.companyName ?? '',
      businessUnitId: employee.businessUnitId ?? null,
      description: employee.description ?? ''
    };
    this.employeeDialogRef.nativeElement.showModal();
  }

  closeDialog(): void {
    this.employeeDialogRef.nativeElement.close();
    this.formErrors.set({});
  }

  // ── Save (create or update) ───────────────────────────────────────────────────
  save(): void {
    this.formErrors.set({});
    this.savingEmployee.set(true);

    if (this.isEditing()) {
      this.update();
    } else {
      this.create();
    }
  }

  private create(): void {
    const cmd = new CreateEmployeeCommand({
      identityNumber: this.form.identityNumber,
      firstname: this.form.firstname,
      lastname: this.form.lastname,
      personalMobileNumber: this.form.personalMobileNumber || undefined,
      sourceTypeStr: this.form.sourceTypeStr,
      activePassiveCode: this.form.activePassiveCode,
      isTerminated: this.form.isTerminated,
      companyName: this.form.companyName || undefined,
      businessUnitId: this.isAdmin() ? (this.form.businessUnitId ?? undefined) : undefined,
      description: this.form.description
    });

    this.employeesClient.createEmployee(cmd).subscribe({
      next: registrationNumber => {
        this.savingEmployee.set(false);
        this.closeDialog();
        // Refresh the search if a search was already done
        if (this.hasSearched()) this.search();
      },
      error: err => this.handleSaveError(err)
    });
  }

  private update(): void {
    const cmd = new UpdateEmployeeCommand({
      registrationNumber: this.form.registrationNumber,
      identityNumber: this.form.identityNumber,
      firstname: this.form.firstname,
      lastname: this.form.lastname,
      personalMobileNumber: this.form.personalMobileNumber || undefined,
      sourceTypeStr: this.form.sourceTypeStr,
      activePassiveCode: this.form.activePassiveCode,
      isTerminated: this.form.isTerminated,
      companyName: this.form.companyName,
      businessUnitId: this.isAdmin() ? (this.form.businessUnitId ?? undefined) : undefined,
      description: this.form.description
    });

    this.employeesClient.updateEmployee(this.form.registrationNumber, cmd).subscribe({
      next: () => {
        this.savingEmployee.set(false);
        this.closeDialog();
        if (this.hasSearched()) this.search();
      },
      error: err => this.handleSaveError(err)
    });
  }

  private handleSaveError(err: any): void {
    this.savingEmployee.set(false);
    try {
      const parsed = JSON.parse(err.response);
      if (parsed?.errors) {
        this.formErrors.set(parsed.errors);
      } else {
        this.formErrors.set({ _: ['An unexpected error occurred.'] });
      }
    } catch {
      this.formErrors.set({ _: ['An unexpected error occurred.'] });
    }
  }

  fieldError(field: string): string | null {
    const errs = this.formErrors()[field];
    return errs?.length ? errs[0] : null;
  }

  hasAnyError(): boolean {
    return Object.keys(this.formErrors()).length > 0;
  }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  private emptyForm(): EmployeeForm {
    return {
      registrationNumber: '',
      identityNumber: '',
      firstname: '',
      lastname: '',
      personalMobileNumber: '',
      sourceTypeStr: '',
      activePassiveCode: '1',
      isTerminated: false,
      companyName: '',
      businessUnitId: null,
      description: ''
    };
  }
}
